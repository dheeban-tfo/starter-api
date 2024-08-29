using Microsoft.AspNetCore.Http;

namespace starterapi.Services;

public interface ITenantDbContextAccessor
{
    TenantDbContext TenantDbContext { get; }
    void SetTenantDbContext(TenantDbContext context);
}

public class TenantDbContextAccessor : ITenantDbContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantDbContextAccessor> _logger;

    public TenantDbContextAccessor(IHttpContextAccessor httpContextAccessor, ILogger<TenantDbContextAccessor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public TenantDbContext TenantDbContext
    {
        get
        {
            var context = _httpContextAccessor.HttpContext?.Items["TenantDbContext"] as TenantDbContext;
            if (context == null)
            {
                _logger.LogError("TenantDbContext is not available in HttpContext.Items");
                throw new InvalidOperationException("TenantDbContext is not available. Ensure the request has gone through the TenantMiddleware.");
            }
            return context;
        }
    }

    public void SetTenantDbContext(TenantDbContext context)
    {
        _httpContextAccessor.HttpContext.Items["TenantDbContext"] = context;
    }
}