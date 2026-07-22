using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OfficeJukebox.Api.Security;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Contracts.Queue;
using OfficeJukebox.Infrastructure.Music;
using OfficeJukebox.Infrastructure.Music.Auth;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(IMusicProviderRegistry registry) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string provider = "spotify", [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query is required." });
        }

        var catalog = registry.GetCatalog(provider);
        if (catalog is null || !catalog.Capabilities.HasFlag(ProviderCapabilities.Search))
        {
            return BadRequest(new { error = $"Provider {provider} does not support search." });
        }

        try
        {
            // Results carry the provider-native external id, so no URL
            // heuristics are needed here anymore.
            var results = await catalog.SearchAsync(q, limit, cancellationToken);
            var items = results.Select(r => new SearchResultItem(
                provider,
                r.ExternalId,
                r.Track.Name,
                r.Track.Album?.Name,
                r.Track.TrackArtworkUrl,
                r.Track.DurationMilliseconds,
                r.Track.Link)).ToList();
            return Ok(items);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = ex.Message });
        }
    }
}

[ApiController]
[Route("api/playback")]
public sealed class PlaybackController(IPlayerClient playerClient) : ControllerBase
{
    [HttpGet("now-playing")]
    public Task<IActionResult> GetNowPlaying(CancellationToken cancellationToken) =>
        playerClient.GetNowPlayingAsync(cancellationToken).ProxyAsync(cancellationToken);

    [HttpGet("devices")]
    [RequireAdmin]
    public Task<IActionResult> GetDevices([FromQuery] string provider, CancellationToken cancellationToken) =>
        playerClient.GetDevicesAsync(provider, cancellationToken).ProxyAsync(cancellationToken);

    [HttpPut("device")]
    [RequireAdmin]
    public Task<IActionResult> SetDevice([FromBody] SetDeviceRequest request, CancellationToken cancellationToken) =>
        playerClient.SetDeviceAsync(request, cancellationToken).ProxyAsync(cancellationToken);
}

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController(
    IMusicProviderRegistry registry,
    IEnumerable<IProviderAuthService> authServices,
    IProviderTokenService tokenService,
    IOptions<MusicProvidersOptions> musicOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var responses = new List<ProviderInfoResponse>();
        foreach (var provider in registry.ListEnabled())
        {
            // Each provider owns its readiness rules (tokens, API keys, …);
            // no provider-id special cases here.
            var catalog = registry.GetCatalog(provider.Id);
            var authenticated = catalog is not null && await catalog.IsReadyAsync(cancellationToken);
            responses.Add(new ProviderInfoResponse(
                provider.Id,
                provider.DisplayName,
                true,
                authenticated,
                CapabilitiesToArray(provider.Capabilities)));
        }

        return Ok(responses);
    }

    [HttpGet("{providerId}/connect-url")]
    [RequireAdmin]
    public IActionResult GetConnectUrl(string providerId)
    {
        var catalog = registry.GetCatalog(providerId);
        if (catalog?.SetupUrl is not { } setupUrl)
        {
            return NotFound();
        }

        return Ok(new ProviderConnectUrlResponse(setupUrl));
    }

    [HttpPut("{providerId}/connection")]
    [RequireAdmin]
    public async Task<IActionResult> SaveConnection(
        string providerId,
        [FromBody] SaveProviderConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest(new { error = "Connection string is required." });
        }

        var catalog = registry.GetCatalog(providerId);
        if (catalog is null)
        {
            return NotFound();
        }

        var connectionString = request.ConnectionString.Trim();

        // Providers with an OAuth auth service accept a refresh token as their
        // connection string: redeem it immediately so the stored credential is
        // a live access token, not the raw paste.
        var auth = FindAuthService(providerId);
        if (auth is not null)
        {
            var refreshed = await auth.RefreshAccessTokenAsync(connectionString, cancellationToken);
            if (refreshed is not null)
            {
                await tokenService.StoreTokensAsync(providerId, refreshed, fallbackRefreshToken: connectionString, cancellationToken);
                return Ok(new { provider = providerId, authenticated = true });
            }
        }

        await tokenService.StoreConnectionStringAsync(providerId, connectionString, cancellationToken);
        return Ok(new { provider = providerId, authenticated = true });
    }

    [HttpGet("{providerId}/auth")]
    [RequireAdmin]
    public IActionResult StartAuth(string providerId)
    {
        var auth = FindAuthService(providerId);
        if (auth is null)
        {
            return NotFound();
        }

        try
        {
            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString($"oauth_state_{providerId}", state);
            return Redirect(auth.BuildAuthorizationUrl(state));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{providerId}/callback")]
    public async Task<IActionResult> Callback(
        string providerId,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var webAppUrl = musicOptions.Value.WebAppUrl ?? "http://localhost:5173";
        var adminUrl = $"{webAppUrl.TrimEnd('/')}/settings/accounts";

        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect($"{adminUrl}?provider={providerId}&auth=error&message={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Redirect($"{adminUrl}?provider={providerId}&auth=error&message=missing_code");
        }

        var expected = HttpContext.Session.GetString($"oauth_state_{providerId}");
        if (string.IsNullOrWhiteSpace(expected) || expected != state)
        {
            return Redirect($"{adminUrl}?provider={providerId}&auth=error&message=invalid_state");
        }

        var auth = FindAuthService(providerId);
        if (auth is null)
        {
            return NotFound();
        }

        try
        {
            var tokens = await auth.CompleteAuthorizationAsync(code, cancellationToken);
            await tokenService.StoreTokensAsync(providerId, tokens, fallbackRefreshToken: null, cancellationToken);
            return Redirect($"{adminUrl}?provider={providerId}&auth=success");
        }
        catch (Exception ex)
        {
            return Redirect($"{adminUrl}?provider={providerId}&auth=error&message={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpDelete("{providerId}/connection")]
    [RequireAdmin]
    public async Task<IActionResult> Disconnect(string providerId, CancellationToken cancellationToken)
    {
        var catalog = registry.GetCatalog(providerId);
        if (catalog is null)
        {
            return NotFound();
        }

        await tokenService.DisconnectAsync(providerId, cancellationToken);
        return Ok(new { provider = providerId, authenticated = false });
    }

    private IProviderAuthService? FindAuthService(string providerId) =>
        authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    private static string[] CapabilitiesToArray(ProviderCapabilities capabilities) =>
        Enum.GetValues<ProviderCapabilities>()
            .Where(c => c != ProviderCapabilities.None && capabilities.HasFlag(c))
            .Select(c => c.ToString())
            .ToArray();
}
