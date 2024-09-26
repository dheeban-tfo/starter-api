using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace starterapi;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantDbContext _context;

    public PermissionMiddleware(RequestDelegate next, TenantDbContext context)
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
                var moduleName = moduleAttribute.Name.ToString();
                var actionName = permissionAttribute.Name;

                var hasPermission = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role)
                    .SelectMany(r => r.AllowedActions)
                    .AnyAsync(ma => ma.Module.Name == moduleName && ma.Name == actionName);

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