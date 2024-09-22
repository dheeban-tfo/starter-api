using starterapi;
using starterapi.Services;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        _logger.LogInformation("TenantMiddleware invoked for path: {Path}", context.Request.Path);

        // Log the authentication status
        _logger.LogInformation(
            "IsAuthenticated: {IsAuthenticated}",
            context.User.Identity?.IsAuthenticated
        );

        // Log the Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        _logger.LogInformation("Authorization Header: {AuthHeader}", authHeader);

        // Log all headers
        foreach (var header in context.Request.Headers)
        {
            _logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
        }

        string tenantId = null;

        if (
            context.Request.Path.StartsWithSegments("/api/auth/login")
            && context.Request.Method == "POST"
        )
        {
            tenantId = context.Request.Headers["X-TenantId"].FirstOrDefault();
            _logger.LogInformation("Login request. TenantId from header: {TenantId}", tenantId);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            tenantId = context.User.FindFirst("TenantId")?.Value;
            _logger.LogInformation(
                "Authenticated request. TenantId from token: {TenantId}",
                tenantId
            );
        }

        _logger.LogInformation("TenantMiddleware invoked for path: {Path}", context.Request.Path);

        if (!string.IsNullOrEmpty(tenantId))
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
                var contextAccessor =
                    scope.ServiceProvider.GetRequiredService<ITenantDbContextAccessor>();
                try
                {
                    var tenant = await tenantService.GetTenantAsync(tenantId);
                    if (tenant != null)
                    {
                        var dbContext1 = tenantService.CreateTenantDbContext(
                            tenant.ConnectionString
                        );
                        contextAccessor.SetTenantDbContext(dbContext1);
                        _logger.LogInformation(
                            "TenantDbContext created for TenantId: {TenantId}",
                            tenantId
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Tenant not found for id: {TenantId}", tenantId);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid tenant identifier");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to create TenantDbContext for TenantId: {TenantId}",
                        tenantId
                    );
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync(
                        "An error occurred while processing the request"
                    );
                    return;
                }
            }
        }
        else
        {
            _logger.LogWarning("No TenantId found in request");
        }

        await _next(context);

        if (context.Items["TenantDbContext"] is TenantDbContext dbContext)
        {
            await dbContext.DisposeAsync();
            _logger.LogInformation("TenantDbContext disposed");
        }
    }
}
