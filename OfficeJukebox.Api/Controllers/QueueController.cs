using Microsoft.AspNetCore.Mvc;
using OfficeJukebox.Api.Security;
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
    [RequireUser]
    public Task<IActionResult> QueueTrack([FromBody] QueueTrackClientRequest request, CancellationToken cancellationToken) =>
        playerClient.QueueTrackAsync(
            new QueueTrackRequest(
                SessionEmail,
                request.Provider,
                request.ExternalId,
                request.TrackName,
                request.AlbumName,
                request.ExternalLink,
                request.Reason),
            cancellationToken).ProxyAsync(cancellationToken);

    [HttpPost("{id:guid}/veto")]
    [RequireUser]
    public Task<IActionResult> Veto(Guid id, CancellationToken cancellationToken) =>
        playerClient.VetoAsync(id, new VetoRequest(SessionEmail), cancellationToken).ProxyAsync(cancellationToken);

    [HttpPost("{id:guid}/skip")]
    [RequireUser]
    public Task<IActionResult> Skip(Guid id, CancellationToken cancellationToken) =>
        playerClient.SkipAsync(id, new SkipRequest(SessionEmail), cancellationToken).ProxyAsync(cancellationToken);

    // [RequireUser] guarantees the key is present; identity always comes from
    // the session, never from the request body.
    private string SessionEmail => HttpContext.Session.GetString(SessionController.EmailKey)!;
}
