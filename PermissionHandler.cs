using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace starterapi;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Module { get; }
    public string Permission { get; }

    public PermissionRequirement(string module, string permission)
    {
        Module = module;
        Permission = permission;
    }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            context.Fail();
            return;
        }

        var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint();
        var moduleAttribute = endpoint?.Metadata.GetMetadata<ModuleAttribute>();
        var permissionAttribute = endpoint?.Metadata.GetMetadata<PermissionAttribute>();

        if (moduleAttribute == null || permissionAttribute == null)
        {
            context.Succeed(requirement);
            return;
        }

        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == int.Parse(userId))
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var hasPermission = await _context.RoleModulePermissions
            .AnyAsync(rmp => 
                userRoles.Contains(rmp.RoleId) && 
                rmp.Module.Name == moduleAttribute.Name && 
                rmp.Permission == permissionAttribute.Name);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}