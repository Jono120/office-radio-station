using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeJukebox.Api.Controllers;

namespace OfficeJukebox.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isAdmin = context.HttpContext.Session.GetString(AdminController.SessionKey) == "true";
        if (!isAdmin)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Admin access required." });
        }
    }
}
