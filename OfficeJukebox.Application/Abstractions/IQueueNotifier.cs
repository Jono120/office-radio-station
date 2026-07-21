namespace OfficeJukebox.Application.Abstractions;

public interface IQueueNotifier
{
    Task NotifyQueueChangedAsync(CancellationToken cancellationToken = default);
    Task NotifyNowPlayingChangedAsync(CancellationToken cancellationToken = default);
    Task NotifyPlaybackProgressAsync(int progressMs, int durationMs, bool isPlaying, CancellationToken cancellationToken = default);
}

public sealed class NullQueueNotifier : IQueueNotifier
{
    public Task NotifyQueueChangedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyNowPlayingChangedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPlaybackProgressAsync(int progressMs, int durationMs, bool isPlaying, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
