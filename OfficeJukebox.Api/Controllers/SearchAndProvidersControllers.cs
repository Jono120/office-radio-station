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
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = ex.Message });
        }
    }

    private static string ExtractExternalId(string provider, Domain.Entities.Track track)
    {
        if (provider.Equals("spotify", StringComparison.OrdinalIgnoreCase) && track.Link.Contains("/track/", StringComparison.Ordinal))
        {
            return track.Link.Split('/').LastOrDefault() ?? track.Name;
        }

        if (provider.Equals("youtube", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractYouTubeVideoId(track.Link) ?? track.Name;
        }

        return track.Name;
    }

    private static string? ExtractYouTubeVideoId(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        if (!uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var query = uri.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 &&
                keyValue[0].Equals("v", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return null;
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
    [RequireAdmin]
    public async Task<IActionResult> GetDevices([FromQuery] string provider, CancellationToken cancellationToken)
    {
        var response = await playerClient.GetDevicesAsync(provider, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    [HttpPut("device")]
    [RequireAdmin]
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
    IProviderTokenService tokenService,
    IOptions<MusicProvidersOptions> musicOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var providers = registry.ListEnabled();
        var responses = new List<ProviderInfoResponse>();
        foreach (var provider in providers)
        {
            var authenticated = await IsProviderReadyAsync(provider, cancellationToken);
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
        if (providerId.Equals("youtube", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ProviderConnectUrlResponse("https://console.cloud.google.com/apis/credentials"));
        }

        if (providerId.Equals("spotify", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ProviderConnectUrlResponse("https://developer.spotify.com/dashboard"));
        }

        var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        if (auth is null)
        {
            return NotFound();
        }

        try
        {
            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString($"oauth_state_{providerId}", state);
            return Ok(new ProviderConnectUrlResponse(auth.BuildAuthorizationUrl(state)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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

        if (providerId.Equals("spotify", StringComparison.OrdinalIgnoreCase))
        {
            var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
            if (auth is not null)
            {
                var refreshed = await auth.RefreshAccessTokenAsync(connectionString, cancellationToken);
                if (refreshed is not null)
                {
                    var expiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
                    await tokenService.StoreTokensAsync(
                        providerId,
                        refreshed.AccessToken,
                        refreshed.RefreshToken ?? connectionString,
                        expiresAt,
                        refreshed.Scope,
                        cancellationToken);
                    return Ok(new { provider = providerId, authenticated = true });
                }
            }
        }

        await tokenService.StoreConnectionStringAsync(providerId, connectionString, cancellationToken);
        return Ok(new { provider = providerId, authenticated = true });
    }

    [HttpGet("{providerId}/auth")]
    [RequireAdmin]
    public IActionResult StartAuth(string providerId)
    {
        var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
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

        var auth = authServices.FirstOrDefault(a => a.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        if (auth is null)
        {
            return NotFound();
        }

        try
        {
            var tokens = await auth.CompleteAuthorizationAsync(code, cancellationToken);
            var expiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
            await tokenService.StoreTokensAsync(
                providerId,
                tokens.AccessToken,
                tokens.RefreshToken,
                expiresAt,
                tokens.Scope,
                cancellationToken);
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

    private async Task<bool> IsProviderReadyAsync(Application.Abstractions.Music.ProviderInfo provider, CancellationToken cancellationToken)
    {
        if (provider.Capabilities.HasFlag(ProviderCapabilities.RequiresAuth))
        {
            return await tokenService.IsAuthenticatedAsync(provider.Id, cancellationToken);
        }

        if (provider.Id.Equals("youtube", StringComparison.OrdinalIgnoreCase))
        {
            if (await tokenService.IsAuthenticatedAsync(provider.Id, cancellationToken))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(musicOptions.Value.YouTube.ApiKey);
        }

        return true;
    }

    private static string[] CapabilitiesToArray(ProviderCapabilities capabilities) =>
        Enum.GetValues<ProviderCapabilities>()
            .Where(c => c != ProviderCapabilities.None && capabilities.HasFlag(c))
            .Select(c => c.ToString())
            .ToArray();
}
