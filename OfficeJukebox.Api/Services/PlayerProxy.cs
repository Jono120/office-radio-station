using Microsoft.AspNetCore.Mvc;

namespace OfficeJukebox.Api.Services;

/// <summary>
/// Relays a Player response to the Api caller as-is: same status code, same
/// JSON body. Every proxying controller action funnels through here so the
/// Player's status codes always propagate (previously two actions used
/// Content(), which flattened Player errors into 200s).
/// </summary>
public static class PlayerProxy
{
    public static async Task<IActionResult> ProxyAsync(
        this Task<HttpResponseMessage> responseTask,
        CancellationToken cancellationToken)
    {
        using var response = await responseTask;
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
