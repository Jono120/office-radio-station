using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeJukebox.Api.Controllers;

namespace OfficeJukebox.Api.Security;

/// <summary>
/// Gates write endpoints (queue, veto, skip) on a signed-in user session.
/// Read endpoints stay open to the LAN so a wall display needs no login.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireUserAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var email = context.HttpContext.Session.GetString(SessionController.EmailKey);
        if (string.IsNullOrWhiteSpace(email))
        {
            context.Result = new UnauthorizedObjectResult(
                new { error = "Sign in with your work email to queue or vote." });
        }
    }
}
