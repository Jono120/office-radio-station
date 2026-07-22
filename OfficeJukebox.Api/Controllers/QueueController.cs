using Microsoft.AspNetCore.Mvc;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Contracts.Queue;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QueueController(IPlayerClient playerClient) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetQueue(CancellationToken cancellationToken) =>
        playerClient.GetQueueAsync(cancellationToken).ProxyAsync(cancellationToken);

    [HttpPost]
    public Task<IActionResult> QueueTrack([FromBody] QueueTrackRequest request, CancellationToken cancellationToken) =>
        playerClient.QueueTrackAsync(request, cancellationToken).ProxyAsync(cancellationToken);

    [HttpPost("{id:guid}/veto")]
    public Task<IActionResult> Veto(Guid id, [FromBody] VetoRequest request, CancellationToken cancellationToken) =>
        playerClient.VetoAsync(id, request, cancellationToken).ProxyAsync(cancellationToken);

    [HttpPost("{id:guid}/skip")]
    public Task<IActionResult> Skip(Guid id, [FromBody] SkipRequest request, CancellationToken cancellationToken) =>
        playerClient.SkipAsync(id, request, cancellationToken).ProxyAsync(cancellationToken);
}
