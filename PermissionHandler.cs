using System.IdentityModel.Tokens.Jwt;
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
     private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionHandler> logger
    )
    {
       _contextFactory = contextFactory;
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
            return;
        }

        _logger.LogInformation($"User is authenticated: {context.User.Identity.IsAuthenticated}");
        _logger.LogInformation($"Number of claims: {context.User.Claims.Count()}");

        // Log the raw token
        var rawToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        _logger.LogInformation($"Raw token: {rawToken}");

        // Manually decode the token
        if (!string.IsNullOrEmpty(rawToken))
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(rawToken) as JwtSecurityToken;

            if (jsonToken != null)
            {
                _logger.LogInformation("Manually decoded token claims:");
                foreach (var claim in jsonToken.Claims)
                {
                    _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
                }
            }
        }


         _logger.LogInformation("All claims in the token:");
        foreach (var claim in context.User.Claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }

           var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        _logger.LogInformation($"User ID from claim: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID is null or empty");
            // foreach (var claim in context.User.Claims)
            // {
            //     _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            // }
            SetUnauthorizedResponse("User not authenticated");
            return;
        }

        var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint();
        var moduleAttribute = endpoint?.Metadata.GetMetadata<ModuleAttribute>();
        var permissionAttribute = endpoint?.Metadata.GetMetadata<PermissionAttribute>();

        _logger.LogInformation($"Module: {moduleAttribute?.Name}, Permission: {permissionAttribute?.Name}");

        if (moduleAttribute == null || permissionAttribute == null)
        {
            _logger.LogWarning("Module or Permission attribute is null");
            //context.Succeed(requirement);
             SetUnauthorizedResponse("Invalid endpoint configuration");
            return;
        }

         using var dbContext = _contextFactory.CreateDbContext();

        var userRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == int.Parse(userId))
            .Select(ur => ur.RoleId)
            .ToListAsync();

        _logger.LogInformation($"User roles: {string.Join(", ", userRoles)}");

        var hasPermission = await dbContext.RoleModulePermissions
            .AnyAsync(rmp => 
                userRoles.Contains(rmp.RoleId) && 
                rmp.Module.Name == moduleAttribute.Name && 
                rmp.Permission == permissionAttribute.Name);

        _logger.LogInformation($"Has permission: {hasPermission}");

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning($"Permission denied for user: {userId}, Module: {moduleAttribute.Name}, Permission: {permissionAttribute.Name}");
            SetUnauthorizedResponse("Insufficient permissions");
        }
    }

     private void SetUnauthorizedResponse(string message)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            _httpContextAccessor.HttpContext.Response.WriteAsync(message);
        }
    }
}
