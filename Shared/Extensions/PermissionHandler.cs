using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using starterapi.Services;
using StarterApi.Helpers;

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
        _logger.LogInformation($"Evaluating authorization for user: {context.User.Identity?.Name}");

       
    _logger.LogInformation("Required permission: {Module}_{Action}", requirement.Module, requirement.Action);


        
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                 _logger.LogWarning("User is not authenticated");
                throw new UserNotAuthenticatedException();
            }

            var userId = Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            

            var tenantId = context.User.FindFirst("TenantId")?.Value;

            if (userId == Guid.Empty || string.IsNullOrEmpty(tenantId))
            {
                throw new MissingTenantIdException();
            }

            //UserContext.CurrentUserId = userId;
            var httpContext = _httpContextAccessor.HttpContext;
            var endpoint = httpContext?.GetEndpoint();
        var moduleAttribute = endpoint?.Metadata.GetMetadata<ModuleAttribute>();
        var permissionAttribute = endpoint?.Metadata.GetMetadata<PermissionAttribute>();

        _logger.LogInformation("Module attribute: {ModuleAttribute}, Permission attribute: {PermissionAttribute}", 
            moduleAttribute?.Name, permissionAttribute?.Name);

        if (moduleAttribute == null || permissionAttribute == null)
        {
            _logger.LogWarning("Module or Permission attribute is missing");
            throw new MissingModuleOrPermissionAttributeException();
        }

            var moduleName = moduleAttribute.Name.ToString();
            var actionName = permissionAttribute.Name;

            _logger.LogInformation(
                "Checking permission for user: {UserId}, Tenant: {TenantId}, Module: {ModuleName}, Action: {ActionName}",
                userId,
                tenantId,
                moduleName,
                actionName
            );

            var cacheKey = $"UserPermissions_{tenantId}_{userId}";
            if (!_cache.TryGetValue(cacheKey, out HashSet<string> userPermissions))
            {
                _logger.LogInformation(
                    "User permissions not found in cache. Fetching from database"
                );
                userPermissions = await GetUserPermissionsFromDatabase(userId, tenantId);
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(
                    TimeSpan.FromMinutes(30)
                );
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
                throw new InsufficientPermissionsException(
                    userId,
                    tenantId,
                    moduleName,
                    actionName
                );
            }
        
        // catch (AuthorizationException ex)
        // {
        //     _logger.LogWarning(ex, "Authorization failed");
        //     SetUnauthorizedResponse(ex.Message);
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "An unexpected error occurred during permission check");
        //     SetUnauthorizedResponse("An unexpected error occurred during permission check");
        // }
    }

    private async Task<HashSet<string>> GetUserPermissionsFromDatabase(
        Guid userId,
        string tenantId
    )
    {
        var dbContext = _contextAccessor.TenantDbContext;

        var permissions = await dbContext
            .UserRoles.Where(ur => ur.UserId == userId)
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

public class AuthorizationException : Exception
{
    public AuthorizationException(string message)
        : base(message) { }
}

public class UserNotAuthenticatedException : AuthorizationException
{
    public UserNotAuthenticatedException()
        : base("User is not authenticated.") { }
}

public class MissingTenantIdException : AuthorizationException
{
    public MissingTenantIdException()
        : base("Tenant ID is missing.") { }
}

public class MissingModuleOrPermissionAttributeException : AuthorizationException
{
    public MissingModuleOrPermissionAttributeException()
        : base("Module or Permission attribute is missing.") { }
}

public class InsufficientPermissionsException : AuthorizationException
{
    public InsufficientPermissionsException(
        Guid userId,
        string tenantId,
        string moduleName,
        string actionName
    )
        : base(
            $"User {userId} in tenant {tenantId} does not have permission for {moduleName}.{actionName}."
        ) { }
}
