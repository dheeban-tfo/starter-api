using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace starterapi;

public class AuthorizePermissionAttribute : TypeFilterAttribute
{
    public AuthorizePermissionAttribute(string module, string permission)
        : base(typeof(PermissionAuthorizeFilter))
    {
        Arguments = new object[] { module, permission };
    }
}

public class PermissionAuthorizeFilter : IAuthorizationFilter
{
    private readonly string _module;
    private readonly string _permission;
    private readonly TenantDbContext _context;

    public PermissionAuthorizeFilter(string module, string permission, TenantDbContext context)
    {
        _module = module;
        _permission = permission;
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

        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
        var userRoles = _context
            .UserRoles.Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToList();

        var hasPermission = _context.RoleModulePermissions.Any(rmp =>
            userRoles.Contains(rmp.RoleId)
            && rmp.Module.Name == _module
            && rmp.Permission == _permission
        );

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
