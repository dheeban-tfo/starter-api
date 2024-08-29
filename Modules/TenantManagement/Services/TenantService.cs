using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using starterapi.Models;

namespace starterapi.Services;

public interface ITenantService
{
    Task<Tenant> GetTenantAsync(string identifier);
    TenantDbContext CreateTenantDbContext(string connectionString);
}

public class TenantService : ITenantService
{
    private readonly TenantManagementDbContext _tenantManagementContext;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        TenantManagementDbContext tenantManagementContext,
        ILogger<TenantService> logger
    )
    {
        _tenantManagementContext = tenantManagementContext;
        _logger = logger;
    }

    public async Task<Tenant> GetTenantAsync(string identifier)
    {
        return await _tenantManagementContext.Tenants.FirstOrDefaultAsync(t =>
            t.Identifier == identifier
        );
    }

    public TenantDbContext CreateTenantDbContext(string connectionString)
    {
        _logger.LogInformation(
            $"Creating TenantDbContext with connection string: {connectionString}"
        );

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string is null or empty",
                nameof(connectionString)
            );
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new TenantDbContext(optionsBuilder.Options);
    }
}
