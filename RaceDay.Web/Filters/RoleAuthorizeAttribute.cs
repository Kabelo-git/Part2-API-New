using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var token = session.GetString(SessionKeys.Role);

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_role != null && session.GetString(SessionKeys.Role) != _role)
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
