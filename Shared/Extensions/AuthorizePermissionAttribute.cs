using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace starterapi;

public class AuthorizePermissionAttribute : TypeFilterAttribute
{
    public AuthorizePermissionAttribute(ModuleName module, string action)
        : base(typeof(PermissionAuthorizeFilter))
    {
        Arguments = new object[] { module, action };
    }
}

public class PermissionAuthorizeFilter : IAuthorizationFilter
{
    private readonly ModuleName _module;
    private readonly string _action;
    private readonly TenantDbContext _context;

    public PermissionAuthorizeFilter(ModuleName module, string action, TenantDbContext context)
    {
        _module = module;
        _action = action;
        _context = context;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
        var hasPermission = _context
            .UserRoles.Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .SelectMany(r => r.AllowedActions)
            .Any(ma => ma.Module.Name == _module.ToString() && ma.Name == _action);

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
