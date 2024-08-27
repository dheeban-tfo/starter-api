using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using starterapi.Services;

namespace starterapi.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        _logger.LogInformation("TenantMiddleware invoked");


       // Log the Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        _logger.LogInformation($"Authorization Header: {authHeader}");
         _logger.LogInformation($"IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
        _logger.LogInformation($"AuthenticationType: {context.User.Identity?.AuthenticationType}");
        _logger.LogInformation($"Name: {context.User.Identity?.Name}");

        if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated");
            
            var tenantId = context.User.FindFirst("TenantId")?.Value;
            
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.User.FindFirst(c => c.Type.EndsWith("TenantId"))?.Value;
            }

            _logger.LogInformation($"TenantId from token: {tenantId}");

            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogInformation($"Attempting to get tenant with id: {tenantId}");
                var tenant = await tenantService.GetTenantAsync(tenantId);
                if (tenant != null)
                {
                    _logger.LogInformation($"Tenant found. Creating TenantDbContext for tenant: {tenantId}");
                    context.Items["TenantDbContext"] = tenantService.CreateTenantDbContext(tenant.ConnectionString);
                }
                else
                {
                    _logger.LogWarning($"Tenant not found for id: {tenantId}");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid tenant identifier");
                    return;
                }
            }
            else
            {
                _logger.LogWarning("TenantId not found in token");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant identifier is required");
                return;
            }
        }
        else
        {
            _logger.LogWarning("User is not authenticated");
            if (!context.Request.Path.StartsWithSegments("/api/auth"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required");
                return;
            }
        }

        // Log all claims in the token
        _logger.LogInformation("All claims in the token:");
        foreach (var claim in context.User.Claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }

        await _next(context);
    }
}