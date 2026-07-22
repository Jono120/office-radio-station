using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Contracts.Queue;

namespace OfficeJukebox.Player.Services;

public sealed class HttpQueueNotifier(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpQueueNotifier> logger) : IQueueNotifier
{
    public Task NotifyQueueChangedAsync(CancellationToken cancellationToken = default) =>
        PostAsync("/api/internal/queue-changed", payload: null, cancellationToken);

    public Task NotifyNowPlayingChangedAsync(CancellationToken cancellationToken = default) =>
        PostAsync("/api/internal/now-playing-changed", payload: null, cancellationToken);

    public Task NotifyPlaybackProgressAsync(int progressMs, int durationMs, bool isPlaying, CancellationToken cancellationToken = default) =>
        PostAsync("/api/internal/playback-progress", new PlaybackProgressEvent(progressMs, durationMs, isPlaying), cancellationToken);

    private async Task PostAsync(string path, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("api-notifier");
            using var response = payload is null
                ? await client.PostAsync(path, null, cancellationToken)
                : await client.PostAsJsonAsync(path, payload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Notifications are best-effort: the state change has already been
            // persisted, so a failed push to the Api must never fail the caller.
            logger.LogWarning(ex, "Failed to notify Api at {Path}", path);
        }
    }
}
