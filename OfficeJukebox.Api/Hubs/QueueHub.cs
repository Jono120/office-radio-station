using Microsoft.AspNetCore.SignalR;

namespace OfficeJukebox.Api.Hubs;

public sealed class QueueHub : Hub
{
    public Task Subscribe() => Groups.AddToGroupAsync(Context.ConnectionId, "queue-updates");

    public Task NotifyQueueChanged() =>
        Clients.Group("queue-updates").SendAsync("QueueUpdated");
}
