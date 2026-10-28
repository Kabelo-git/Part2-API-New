using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId == null)
        {
            context.Result = new ObjectResult(new { message = "You must log in first." }) { StatusCode = 401 };
            return;
        }

        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new ObjectResult(new { message = $"This action is only available to the {_requiredRole} role." }) { StatusCode = 403 };
        }
    }
}