﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    public PermissionHandler(
        ITenantDbContextAccessor contextAccessor,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionHandler> logger,
        IMemoryCache cache
    )
    {
        _contextAccessor = contextAccessor;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _cache = cache;
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
        var tenantId = context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("User ID or Tenant ID is null or empty");
            SetUnauthorizedResponse("User not authenticated or tenant not identified");
            return;
        }

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

            _logger.LogInformation("Checking permission for user: {UserId}, Tenant: {TenantId}, Module: {ModuleName}, Action: {ActionName}", userId, tenantId, moduleName, actionName);

            var cacheKey = $"UserPermissions_{tenantId}_{userId}";
            if (!_cache.TryGetValue(cacheKey, out HashSet<string> userPermissions))
            {
                 _logger.LogInformation("User permissions not found in cache. Fetching from database");
                userPermissions = await GetUserPermissionsFromDatabase(userId, tenantId);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                _cache.Set(cacheKey, userPermissions, cacheEntryOptions);
                _logger.LogInformation("User permissions cached successfully");
            }

            var hasPermission = userPermissions.Contains($"{moduleName}_{actionName}");

            _logger.LogInformation("Has permission: {HasPermission}", hasPermission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    "Permission denied for user: {UserId}, Tenant: {TenantId}, Module: {ModuleName}, Action: {ActionName}",
                    userId,
                    tenantId,
                    moduleName,
                    actionName
                );
                SetUnauthorizedResponse("Insufficient permissions");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking permissions for user: {UserId}, Tenant: {TenantId}", userId, tenantId);
            SetUnauthorizedResponse("An error occurred while checking permissions");
        }
    }

    private async Task<HashSet<string>> GetUserPermissionsFromDatabase(string userId, string tenantId)
    {
        var dbContext = _contextAccessor.TenantDbContext;

        var permissions = await dbContext.UserRoles
            .Where(ur => ur.UserId == int.Parse(userId))
            .SelectMany(ur => ur.Role.AllowedActions)
            .Where(ma => ma.Module != null)
            .Select(ma => $"{ma.Module.Name}_{ma.Name}")
            .ToListAsync();

        return new HashSet<string>(permissions);
    }

    private void SetUnauthorizedResponse(string message)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            _httpContextAccessor.HttpContext.Response.WriteAsync(message);
            _logger.LogError(message);
        }
    }
}
