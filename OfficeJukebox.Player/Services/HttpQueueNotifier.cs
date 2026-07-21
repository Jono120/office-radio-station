using OfficeJukebox.Application.Abstractions;

namespace OfficeJukebox.Player.Services;

public sealed class HttpQueueNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IQueueNotifier
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("api-notifier");
        var baseUrl = configuration["Api:NotifyBaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        return client;
    }

    public Task NotifyQueueChangedAsync(CancellationToken cancellationToken = default) =>
        CreateClient().PostAsync("/api/internal/queue-changed", null, cancellationToken);

    public Task NotifyNowPlayingChangedAsync(CancellationToken cancellationToken = default) =>
        CreateClient().PostAsync("/api/internal/now-playing-changed", null, cancellationToken);

    public Task NotifyPlaybackProgressAsync(int progressMs, int durationMs, bool isPlaying, CancellationToken cancellationToken = default) =>
        CreateClient().PostAsJsonAsync("/api/internal/playback-progress", new { progressMs, durationMs, isPlaying }, cancellationToken);
}
