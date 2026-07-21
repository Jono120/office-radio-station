using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OfficeJukebox.Api.Options;

namespace OfficeJukebox.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IOptions<AdminOptions> options) : ControllerBase
{
    public const string SessionKey = "admin_authenticated";

    [HttpGet("me")]
    public IActionResult Me() =>
        IsAuthenticated() ? Ok(new { authenticated = true }) : Unauthorized();

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var configuredPassword = options.Value.Password;
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Admin access is not configured." });
        }

        if (!string.Equals(request.Password, configuredPassword, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Invalid admin password." });
        }

        HttpContext.Session.SetString(SessionKey, "true");
        return Ok(new { authenticated = true });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SessionKey);
        return Ok(new { authenticated = false });
    }

    private bool IsAuthenticated() => HttpContext.Session.GetString(SessionKey) == "true";
}

public sealed record AdminLoginRequest(string Password);
