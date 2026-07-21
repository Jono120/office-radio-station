using Microsoft.AspNetCore.Mvc;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Contracts.Queue;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QueueController(IPlayerClient playerClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
    {
        var response = await playerClient.GetQueueAsync(cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Content(content, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> QueueTrack([FromBody] QueueTrackRequest request, CancellationToken cancellationToken)
    {
        var response = await playerClient.QueueTrackAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    [HttpPost("{id:guid}/veto")]
    public async Task<IActionResult> Veto(Guid id, [FromBody] VetoRequest request, CancellationToken cancellationToken)
    {
        var response = await playerClient.VetoAsync(id, request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    [HttpPost("{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, [FromBody] SkipRequest request, CancellationToken cancellationToken)
    {
        var response = await playerClient.SkipAsync(id, request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
