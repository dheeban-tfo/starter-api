using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using starterapi.Services;

namespace starterapi;

public class PermissionRequirement : IAuthorizationRequirement
{
    public ModuleName Module { get; }
    public Enum Action { get; }

    public PermissionRequirement(ModuleName module, Enum action)
    {
        Module = module;
        Action = action;
    }

    public PermissionRequirement() { }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        ITenantDbContextAccessor contextAccessor,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionHandler> logger
    )
    {
        _contextAccessor = contextAccessor;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        _logger.LogInformation("Starting permission check");

        if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            SetUnauthorizedResponse("User not authenticated");
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID is null or empty");
            SetUnauthorizedResponse("User not authenticated");
            return;
        }

        var dbContext = _contextAccessor.TenantDbContext;

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var endpoint = httpContext?.GetEndpoint();
            var moduleAttribute = endpoint?.Metadata.GetMetadata<ModuleAttribute>();
            var permissionAttribute = endpoint?.Metadata.GetMetadata<PermissionAttribute>();

            if (moduleAttribute == null || permissionAttribute == null)
            {
                _logger.LogWarning("Module or Permission attribute is missing");
                SetUnauthorizedResponse("Invalid permission configuration");
                return;
            }

            var moduleName = moduleAttribute.Name.ToString();
            var actionName = permissionAttribute.Name;

            _logger.LogInformation($"Checking permission for user: {userId}, Module: {moduleName}, Action: {actionName}");

            var hasPermission = await dbContext.UserRoles
                .Where(ur => ur.UserId == int.Parse(userId))
                .SelectMany(ur => ur.Role.AllowedActions)
                .AnyAsync(ma =>
                    ma.Module != null &&
                    ma.Module.Name == moduleName &&
                    ma.Name == actionName
                );

            _logger.LogInformation($"Has permission: {hasPermission}");

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    $"Permission denied for user: {userId}, Module: {moduleName}, Action: {actionName}"
                );
                SetUnauthorizedResponse("Insufficient permissions");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while checking permissions for user: {userId}");
            SetUnauthorizedResponse("An error occurred while checking permissions");
        }
    }

    private void SetUnauthorizedResponse(string message)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
            _httpContextAccessor.HttpContext.Response.WriteAsync(message);
            _logger.LogError(message);
        }
    }
}
