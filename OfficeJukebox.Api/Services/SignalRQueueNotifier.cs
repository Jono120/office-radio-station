using Microsoft.AspNetCore.SignalR;
using OfficeJukebox.Api.Hubs;
using OfficeJukebox.Application.Abstractions;

namespace OfficeJukebox.Api.Services;

public sealed class SignalRQueueNotifier(IHubContext<QueueHub> hubContext) : IQueueNotifier
{
    public Task NotifyQueueChangedAsync(CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("QueueChanged", cancellationToken);

    public Task NotifyNowPlayingChangedAsync(CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("NowPlayingChanged", cancellationToken);

    public Task NotifyPlaybackProgressAsync(int progressMs, int durationMs, bool isPlaying, CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("PlaybackProgress", new { progressMs, durationMs, isPlaying }, cancellationToken);
}
