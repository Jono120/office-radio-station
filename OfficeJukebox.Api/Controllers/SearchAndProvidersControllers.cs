using Microsoft.AspNetCore.Mvc;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Contracts.Queue;
using OfficeJukebox.Infrastructure.Music.Auth;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(IMusicProviderRegistry registry) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string provider = "spotify", [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var catalog = registry.GetCatalog(provider);
        if (catalog is null || !catalog.Capabilities.HasFlag(ProviderCapabilities.Search))
        {
            return BadRequest(new { error = $"Provider {provider} does not support search." });
        }

        var results = await catalog.SearchAsync(q, limit, cancellationToken);
        var items = results.Select(t => new SearchResultItem(
            provider,
            ExtractExternalId(provider, t),
            t.Name,
            t.Album?.Name,
            t.TrackArtworkUrl,
            t.DurationMilliseconds,
            t.Link)).ToList();
        return Ok(items);
    }

    private static string ExtractExternalId(string provider, Domain.Entities.Track track)
    {
        if (provider.Equals("spotify", StringComparison.OrdinalIgnoreCase) && track.Link.Contains("/track/", StringComparison.Ordinal))
        {
            return track.Link.Split('/').LastOrDefault() ?? track.Name;
        }

        return track.Name;
    }
}

[ApiController]
[Route("api/playback")]
public sealed class PlaybackController(IPlayerClient playerClient) : ControllerBase
{
    [HttpGet("now-playing")]
    public async Task<IActionResult> GetNowPlaying(CancellationToken cancellationToken)
    {
        var response = await playerClient.GetNowPlayingAsync(cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Content(content, "application/json");
    }

    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices([FromQuery] string provider, CancellationToken cancellationToken)
    {
        var response = await playerClient.GetDevicesAsync(provider, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Content(content, "application/json");
    }

    [HttpPut("device")]
    public async Task<IActionResult> SetDevice([FromBody] SetDeviceRequest request, CancellationToken cancellationToken)
    {
        var response = await playerClient.SetDeviceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController(
    IMusicProviderRegistry registry,
    IEnumerable<IProviderAuthService> authServices,
    IProviderTokenService tokenService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var providers = registry.ListEnabled();
        var responses = new List<ProviderInfoResponse>();
        foreach (var provider in providers)
        {
            var authenticated = await tokenService.IsAuthenticatedAsync(provider.Id, cancellationToken);
            responses.Add(new ProviderInfoResponse(
                provider.Id,
                provider.DisplayName,
                true,
                authenticated,
                CapabilitiesToArray(provider.Capabilities)));
        }

        return Ok(responses);
    }

    [HttpGet("{providerId}/auth")]
    public IActionResult StartAuth(string providerId)
    {
        var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        if (auth is null)
        {
            return NotFound();
        }

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString($"oauth_state_{providerId}", state);
        return Redirect(auth.BuildAuthorizationUrl(state));
    }

    [HttpGet("{providerId}/callback")]
    public async Task<IActionResult> Callback(string providerId, [FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var expected = HttpContext.Session.GetString($"oauth_state_{providerId}");
        if (string.IsNullOrWhiteSpace(expected) || expected != state)
        {
            return BadRequest(new { error = "Invalid OAuth state." });
        }

        var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        if (auth is null)
        {
            return NotFound();
        }

        await auth.CompleteAuthorizationAsync(code, cancellationToken);
        return Ok(new { provider = providerId, authenticated = true });
    }

    private static string[] CapabilitiesToArray(ProviderCapabilities capabilities) =>
        Enum.GetValues<ProviderCapabilities>()
            .Where(c => c != ProviderCapabilities.None && capabilities.HasFlag(c))
            .Select(c => c.ToString())
            .ToArray();
}
