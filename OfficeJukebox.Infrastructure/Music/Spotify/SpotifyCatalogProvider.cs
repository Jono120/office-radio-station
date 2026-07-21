using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Domain.ValueObjects;
using OfficeJukebox.Infrastructure.Music.Auth;

namespace OfficeJukebox.Infrastructure.Music.Spotify;

public sealed class SpotifyCatalogProvider(
    IHttpClientFactory httpClientFactory,
    IProviderTokenService tokenService,
    IOptions<MusicProvidersOptions> options) : IMusicCatalogProvider, IMusicPlaybackController
{
    private const string ApiBase = "https://api.spotify.com/v1/";
    public string ProviderId => "spotify";
    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.Search | ProviderCapabilities.Resolve |
        ProviderCapabilities.DevicePlayback | ProviderCapabilities.RequiresAuth;

    public async Task<IReadOnlyList<Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync($"search?q={Uri.EscapeDataString(query)}&type=track&limit={limit}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var items = json.GetProperty("tracks").GetProperty("items");
        return items.EnumerateArray().Select(MapTrack).ToList();
    }

    public async Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync($"tracks/{trackRef.ExternalId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return MapTrack(json);
    }

    public async Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync("me/player", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return new PlaybackState(false, 0, 0, null, null, null);
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var isPlaying = json.GetProperty("is_playing").GetBoolean();
        var progress = json.GetProperty("progress_ms").GetInt32();
        var device = json.GetProperty("device");
        var item = json.GetProperty("item");
        var duration = item.GetProperty("duration_ms").GetInt32();
        var trackRef = new TrackRef(ProviderId, item.GetProperty("id").GetString() ?? string.Empty);
        return new PlaybackState(isPlaying, progress, duration, trackRef, device.GetProperty("name").GetString(), device.GetProperty("id").GetString());
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
        response.EnsureSuccessStatusCode();
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.PutAsync("me/player/pause", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.PutAsync("me/player/play", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SkipAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.PostAsync("me/player/next", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<PlaybackDevice>> ListDevicesAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthedClientAsync(cancellationToken);
        var response = await client.GetAsync("me/player/devices", cancellationToken);
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
        options.Value.Spotify.PreferredDeviceId = deviceId;
    }

    private async Task<HttpClient> CreateAuthedClientAsync(CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(ProviderId, cancellationToken)
            ?? throw new InvalidOperationException("Spotify is not authenticated.");
        var client = httpClientFactory.CreateClient("spotify");
        client.BaseAddress = new Uri(ApiBase);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Track MapTrack(JsonElement item) => new()
    {
        Name = item.GetProperty("name").GetString() ?? string.Empty,
        Link = item.GetProperty("external_urls").GetProperty("spotify").GetString() ?? string.Empty,
        DurationMilliseconds = item.GetProperty("duration_ms").GetInt64(),
        TrackArtworkUrl = item.GetProperty("album").GetProperty("images").EnumerateArray().FirstOrDefault().GetProperty("url").GetString(),
        Album = new Album { Name = item.GetProperty("album").GetProperty("name").GetString() ?? string.Empty },
        Artists = item.GetProperty("artists").EnumerateArray()
            .Select(a => new Artist { Name = a.GetProperty("name").GetString() ?? string.Empty })
            .ToList()
    };
}
