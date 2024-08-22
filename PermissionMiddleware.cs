using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace starterapi;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApplicationDbContext _context;

    public PermissionMiddleware(RequestDelegate next, ApplicationDbContext context)
    {
        _next = next;
        _context = context;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var user = httpContext.User;

        if (endpoint != null && user.Identity != null && user.Identity.IsAuthenticated)
        {
            var moduleAttribute = endpoint.Metadata.GetMetadata<ModuleAttribute>();
            var permissionAttribute = endpoint.Metadata.GetMetadata<PermissionAttribute>();

            if (moduleAttribute != null && permissionAttribute != null)
            {
                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                var hasPermission = await _context.RoleModulePermissions
                    .AnyAsync(rmp => userRoles.Contains(rmp.RoleId) && rmp.Module.Name == moduleAttribute.Name && rmp.Permission == permissionAttribute.Name);

                if (!hasPermission)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
        }

        await _next(httpContext);
    }
}
