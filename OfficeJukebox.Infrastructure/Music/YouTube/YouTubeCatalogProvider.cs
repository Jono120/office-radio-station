using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;
using OfficeJukebox.Infrastructure.Music.Auth;

namespace OfficeJukebox.Infrastructure.Music.YouTube;

public sealed partial class YouTubeCatalogProvider(
    IHttpClientFactory httpClientFactory,
    IProviderTokenService tokenService,
    IOptions<MusicProvidersOptions> options) : IMusicCatalogProvider
{
    private const string ApiBase = "https://www.googleapis.com/youtube/v3/";
    private const int MaxSearchLimit = 25;

    public string ProviderId => "youtube";
    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.Search | ProviderCapabilities.Resolve;
    public string? SetupUrl => "https://console.cloud.google.com/apis/credentials";

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        // Ready with either a stored key (saved via provider settings) or a
        // configured MusicProviders:YouTube:ApiKey fallback.
        if (await tokenService.IsAuthenticatedAsync(ProviderId, cancellationToken))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Value.YouTube.ApiKey);
    }

    public async Task<IReadOnlyList<CatalogSearchResult>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        var effectiveLimit = Math.Clamp(limit, 1, MaxSearchLimit);
        var client = CreateClient();
        var url =
            $"search?part=snippet&type=video&videoCategoryId=10&q={Uri.EscapeDataString(query)}&maxResults={effectiveLimit}&key={Uri.EscapeDataString(apiKey)}";
        var response = await client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var items = json.GetProperty("items");
        var videoIds = items.EnumerateArray()
            .Select(item => item.GetProperty("id").GetProperty("videoId").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToList();

        if (videoIds.Count == 0)
        {
            return [];
        }

        var durations = await FetchDurationsAsync(client, apiKey, videoIds, cancellationToken);
        return items.EnumerateArray()
            .Select(item => MapSearchItem(item, durations))
            .Where(result => result is not null)
            .Cast<CatalogSearchResult>()
            .ToList();
    }

    public async Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        var client = CreateClient();
        var url =
            $"videos?part=snippet,contentDetails&id={Uri.EscapeDataString(trackRef.ExternalId)}&key={Uri.EscapeDataString(apiKey)}";
        var response = await client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var items = json.GetProperty("items");
        if (items.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"YouTube video {trackRef.ExternalId} was not found.");
        }

        return MapVideo(items[0]);
    }

    private async Task<Dictionary<string, long>> FetchDurationsAsync(
        HttpClient client,
        string apiKey,
        IReadOnlyList<string> videoIds,
        CancellationToken cancellationToken)
    {
        var ids = string.Join(',', videoIds);
        var url =
            $"videos?part=contentDetails&id={Uri.EscapeDataString(ids)}&key={Uri.EscapeDataString(apiKey)}";
        var response = await client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.GetProperty("items").EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("id").GetString() ?? string.Empty,
                item => ParseIso8601Duration(item.GetProperty("contentDetails").GetProperty("duration").GetString()));
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("youtube");
        client.BaseAddress = new Uri(ApiBase);
        return client;
    }

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        var fromDatabase = await tokenService.GetAccessTokenAsync(ProviderId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDatabase))
        {
            return fromDatabase;
        }

        var apiKey = options.Value.YouTube.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "YouTube is not configured. Add a YouTube Data API key in provider settings.");
        }

        return apiKey;
    }

    private static CatalogSearchResult? MapSearchItem(JsonElement item, IReadOnlyDictionary<string, long> durations)
    {
        var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        var snippet = item.GetProperty("snippet");
        durations.TryGetValue(videoId, out var durationMs);
        return new CatalogSearchResult(videoId, MapSnippet(videoId, snippet, durationMs));
    }

    private static Track MapVideo(JsonElement item)
    {
        var videoId = item.GetProperty("id").GetString() ?? string.Empty;
        var snippet = item.GetProperty("snippet");
        var duration = item.TryGetProperty("contentDetails", out var contentDetails)
            ? ParseIso8601Duration(contentDetails.GetProperty("duration").GetString())
            : 0;
        return MapSnippet(videoId, snippet, duration);
    }

    private static Track MapSnippet(string videoId, JsonElement snippet, long durationMs)
    {
        var channelTitle = snippet.GetProperty("channelTitle").GetString() ?? string.Empty;
        return new Track
        {
            Name = snippet.GetProperty("title").GetString() ?? string.Empty,
            Link = $"https://www.youtube.com/watch?v={videoId}",
            DurationMilliseconds = durationMs,
            TrackArtworkUrl = GetThumbnailUrl(snippet),
            Album = new Album { Name = "YouTube" },
            Artists = [new Artist { Name = channelTitle }]
        };
    }

    private static string? GetThumbnailUrl(JsonElement snippet)
    {
        if (!snippet.TryGetProperty("thumbnails", out var thumbnails))
        {
            return null;
        }

        foreach (var size in new[] { "medium", "high", "default" })
        {
            if (thumbnails.TryGetProperty(size, out var thumbnail) &&
                thumbnail.TryGetProperty("url", out var urlElement))
            {
                return urlElement.GetString();
            }
        }

        return null;
    }

    private static long ParseIso8601Duration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = Iso8601DurationRegex().Match(value);
        if (!match.Success)
        {
            return 0;
        }

        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 0;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        var seconds = match.Groups["seconds"].Success ? int.Parse(match.Groups["seconds"].Value) : 0;
        return ((hours * 60L + minutes) * 60L + seconds) * 1000L;
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
            System.Net.HttpStatusCode.Forbidden =>
                new InvalidOperationException(
                    "YouTube API denied this request. Check that the Data API v3 is enabled and the API key is valid."),
            System.Net.HttpStatusCode.TooManyRequests =>
                new InvalidOperationException("YouTube API quota exceeded. Try again later."),
            _ => new HttpRequestException($"YouTube API request failed ({(int)response.StatusCode}): {body}")
        };
    }

    [GeneratedRegex(@"PT(?:(?<hours>\d+)H)?(?:(?<minutes>\d+)M)?(?:(?<seconds>\d+)S)?", RegexOptions.Compiled)]
    private static partial Regex Iso8601DurationRegex();
}
