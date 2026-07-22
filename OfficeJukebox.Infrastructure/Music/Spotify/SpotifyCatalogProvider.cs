using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;
using OfficeJukebox.Infrastructure.Music.Auth;

namespace OfficeJukebox.Infrastructure.Music.Spotify;

public sealed class SpotifyCatalogProvider(
    IHttpClientFactory httpClientFactory,
    IProviderTokenService tokenService,
    IOptions<MusicProvidersOptions> options) : IMusicCatalogProvider, IMusicPlaybackController
{
    private const string ApiBase = "https://api.spotify.com/v1/";
    private const int MaxSearchLimit = 10;

    public string ProviderId => "spotify";
    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.Search | ProviderCapabilities.Resolve |
        ProviderCapabilities.DevicePlayback | ProviderCapabilities.RequiresAuth;
    public string? SetupUrl => "https://developer.spotify.com/dashboard";

    public async Task<IReadOnlyList<CatalogSearchResult>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit, 1, MaxSearchLimit);
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync(
            $"search?q={Uri.EscapeDataString(query)}&type=track&limit={effectiveLimit}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var items = json.GetProperty("tracks").GetProperty("items");
        return items.EnumerateArray()
            .Select(item => new CatalogSearchResult(
                item.GetProperty("id").GetString() ?? string.Empty,
                MapTrack(item)))
            .ToList();
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default) =>
        tokenService.IsAuthenticatedAsync(ProviderId, cancellationToken);

    public async Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync($"tracks/{trackRef.ExternalId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return MapTrack(json);
    }

    public async Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync("me/player", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new PlaybackState(false, 0, 0, null, null, null);
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var isPlaying = json.TryGetProperty("is_playing", out var isPlayingElement) && isPlayingElement.GetBoolean();
        var progress = json.TryGetProperty("progress_ms", out var progressElement) ? progressElement.GetInt32() : 0;
        var deviceName = json.TryGetProperty("device", out var deviceElement) && deviceElement.ValueKind == JsonValueKind.Object
            ? deviceElement.GetProperty("name").GetString()
            : null;
        var deviceId = json.TryGetProperty("device", out deviceElement) && deviceElement.ValueKind == JsonValueKind.Object
            ? deviceElement.GetProperty("id").GetString()
            : null;

        if (!json.TryGetProperty("item", out var itemElement) || itemElement.ValueKind == JsonValueKind.Null)
        {
            return new PlaybackState(isPlaying, progress, 0, null, deviceName, deviceId);
        }

        var duration = itemElement.TryGetProperty("duration_ms", out var durationElement)
            ? durationElement.GetInt32()
            : 0;
        var trackId = itemElement.GetProperty("id").GetString() ?? string.Empty;
        var trackRef = new TrackRef(ProviderId, trackId);
        return new PlaybackState(isPlaying, progress, duration, trackRef, deviceName, deviceId);
    }

    public async Task PlayAsync(TrackRef trackRef, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var deviceId = options.Value.Spotify.PreferredDeviceId;
        var url = string.IsNullOrWhiteSpace(deviceId)
            ? "me/player/play"
            : $"me/player/play?device_id={Uri.EscapeDataString(deviceId)}";
        var body = JsonSerializer.Serialize(new { uris = new[] { $"spotify:track:{trackRef.ExternalId}" } });
        var response = await client.PutAsync(url, new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("No active Spotify Connect device found. Open Spotify on a speaker, computer, or phone and try again.");
        }

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task SkipAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.PostAsync("me/player/next", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<PlaybackDevice>> ListDevicesAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync("me/player/devices", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.GetProperty("devices").EnumerateArray()
            .Select(d => new PlaybackDevice(
                d.GetProperty("id").GetString() ?? string.Empty,
                d.GetProperty("name").GetString() ?? string.Empty,
                d.GetProperty("is_active").GetBoolean(),
                d.GetProperty("type").GetString()))
            .ToList();
    }

    public async Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var body = JsonSerializer.Serialize(new { device_ids = new[] { deviceId }, play = false });
        var response = await client.PutAsync("me/player", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        options.Value.Spotify.PreferredDeviceId = deviceId;
    }

    private async Task<HttpClient> CreateAuthedClientAsync(CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(ProviderId, cancellationToken)
            ?? throw new InvalidOperationException("Spotify is not connected. Connect Spotify in provider settings first.");
        var client = httpClientFactory.CreateClient("spotify");
        client.BaseAddress = new Uri(ApiBase);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new InvalidOperationException("Spotify authorization expired. Reconnect Spotify in provider settings."),
            HttpStatusCode.Forbidden => new InvalidOperationException("Spotify denied this request. Ensure the Spotify account is allowlisted on your developer app and has Premium if required."),
            _ => new HttpRequestException($"Spotify API request failed ({(int)response.StatusCode}): {body}")
        };
    }

    private static Track MapTrack(JsonElement item)
    {
        var trackId = item.GetProperty("id").GetString() ?? string.Empty;
        var album = item.GetProperty("album");
        string? artworkUrl = null;
        if (album.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in images.EnumerateArray())
            {
                if (image.TryGetProperty("url", out var urlElement))
                {
                    artworkUrl = urlElement.GetString();
                    break;
                }
            }
        }

        return new Track
        {
            Name = item.GetProperty("name").GetString() ?? string.Empty,
            Link = $"https://open.spotify.com/track/{trackId}",
            DurationMilliseconds = item.GetProperty("duration_ms").GetInt64(),
            TrackArtworkUrl = artworkUrl,
            Album = new Album { Name = album.GetProperty("name").GetString() ?? string.Empty },
            Artists = item.GetProperty("artists").EnumerateArray()
                .Select(a => new Artist { Name = a.GetProperty("name").GetString() ?? string.Empty })
                .ToList()
        };
    }
}
