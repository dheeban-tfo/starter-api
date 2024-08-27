using Microsoft.AspNetCore.Http;

namespace starterapi.Services;

public interface ITenantDbContextAccessor
{
    TenantDbContext TenantDbContext { get; }
}

public class TenantDbContextAccessor : ITenantDbContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantDbContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantDbContext TenantDbContext
    {
        get
        {
            var context = _httpContextAccessor.HttpContext?.Items["TenantDbContext"] as TenantDbContext;
            if (context == null)
            {
                throw new InvalidOperationException("TenantDbContext is not available. Ensure it is set before accessing.");
            }
            return context;
        }
    }
}