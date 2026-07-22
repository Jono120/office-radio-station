using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Configuration;

namespace OfficeJukebox.Api.Controllers;

/// <summary>
/// User identity for queueing and voting (remediation plan item 19). A user
/// signs in with their work email + display name; the email's domain must
/// match Organization:Domain. Identity lives in the same cookie session the
/// admin login uses, and write endpoints derive the user from it server-side
/// instead of trusting request bodies.
///
/// Scope note: this validates the email's domain, it does not verify mailbox
/// ownership — combined with LAN-only access that is the agreed bar for the
/// office jukebox prototype.
/// </summary>
[ApiController]
[Route("api/session")]
public sealed class SessionController(IOptions<OrganizationOptions> organization) : ControllerBase
{
    public const string EmailKey = "user_email";
    public const string DisplayNameKey = "user_display_name";

    [HttpGet]
    public IActionResult Me()
    {
        var email = HttpContext.Session.GetString(EmailKey);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        return Ok(new SessionResponse(email, HttpContext.Session.GetString(DisplayNameKey) ?? email));
    }

    [HttpPost]
    public IActionResult SignIn([FromBody] SignInRequest request)
    {
        var domain = organization.Value.Domain;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Sign-in is not configured. Set Organization:Domain." });
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out var parsed))
        {
            return BadRequest(new { error = "Enter a valid email address." });
        }

        if (!parsed.Host.Equals(domain, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { error = $"Use your @{domain} work email to sign in." });
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? parsed.User
            : request.DisplayName.Trim();

        // Canonical identity is the lowercased email; rules and attribution
        // key off it, the display name is presentation only.
        var canonicalEmail = email.ToLowerInvariant();
        HttpContext.Session.SetString(EmailKey, canonicalEmail);
        HttpContext.Session.SetString(DisplayNameKey, displayName);
        return Ok(new SessionResponse(canonicalEmail, displayName));
    }

    [HttpDelete]
    public IActionResult SignOut_()
    {
        HttpContext.Session.Remove(EmailKey);
        HttpContext.Session.Remove(DisplayNameKey);
        return Ok(new { authenticated = false });
    }
}

public sealed record SignInRequest(string? Email, string? DisplayName);

public sealed record SessionResponse(string Email, string DisplayName);
