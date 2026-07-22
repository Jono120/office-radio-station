using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OfficeJukebox.Api.Hubs;
using OfficeJukebox.Contracts.Queue;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/internal")]
public sealed class InternalNotificationsController(IHubContext<QueueHub> hubContext) : ControllerBase
{
    [HttpPost("queue-changed")]
    public async Task<IActionResult> QueueChanged(CancellationToken cancellationToken)
    {
        await hubContext.Clients.All.SendAsync("QueueChanged", cancellationToken);
        return Ok();
    }

    [HttpPost("now-playing-changed")]
    public async Task<IActionResult> NowPlayingChanged(CancellationToken cancellationToken)
    {
        await hubContext.Clients.All.SendAsync("NowPlayingChanged", cancellationToken);
        return Ok();
    }

    [HttpPost("playback-progress")]
    public async Task<IActionResult> PlaybackProgress([FromBody] PlaybackProgressEvent payload, CancellationToken cancellationToken)
    {
        await hubContext.Clients.All.SendAsync("PlaybackProgress", payload, cancellationToken);
        return Ok();
    }
}
