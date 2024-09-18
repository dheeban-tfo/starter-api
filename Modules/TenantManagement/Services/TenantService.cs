using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using starterapi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace starterapi.Services
{
    public interface ITenantService
    {
        Task<Tenant> GetTenantAsync(string identifier);
        Task<IEnumerable<Tenant>> GetAllTenantsAsync();
        Task<Tenant> CreateTenantAsync(Tenant tenant);
        Task UpdateTenantAsync(Tenant tenant);
        Task DeleteTenantAsync(string identifier);
        Task MigrateTenantDatabaseAsync(string identifier);
        Task MigrateAllTenantDatabasesAsync();
        TenantDbContext CreateTenantDbContext(string connectionString);
    }

    public class TenantService : ITenantService
    {
        private readonly TenantManagementDbContext _tenantManagementContext;
        private readonly ILogger<TenantService> _logger;

        public TenantService(TenantManagementDbContext tenantManagementContext, ILogger<TenantService> logger)
        {
            _tenantManagementContext = tenantManagementContext;
            _logger = logger;
        }

        public async Task<Tenant> GetTenantAsync(string identifier)
        {
            return await _tenantManagementContext.Tenants
                .FirstOrDefaultAsync(t => t.Identifier == identifier);
        }

        public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
        {
            return await _tenantManagementContext.Tenants.ToListAsync();
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant)
        {
            _logger.LogInformation($"Creating new tenant: {tenant.Name}");

            _tenantManagementContext.Tenants.Add(tenant);
            await _tenantManagementContext.SaveChangesAsync();

            // Create and migrate the new tenant's database
            await MigrateTenantDatabaseAsync(tenant.Identifier);

            return tenant;
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            _tenantManagementContext.Tenants.Update(tenant);
            await _tenantManagementContext.SaveChangesAsync();
        }

        public async Task DeleteTenantAsync(string identifier)
        {
            var tenant = await GetTenantAsync(identifier);
            if (tenant != null)
            {
                _tenantManagementContext.Tenants.Remove(tenant);
                await _tenantManagementContext.SaveChangesAsync();
            }
        }

        public async Task MigrateTenantDatabaseAsync(string identifier)
        {
            var tenant = await GetTenantAsync(identifier);
            if (tenant == null)
            {
                throw new ArgumentException($"Tenant with identifier {identifier} not found.");
            }

            _logger.LogInformation($"Migrating database for tenant: {tenant.Name}");

            var context = CreateTenantDbContext(tenant.ConnectionString);

            // Check if the database exists
            if (!await context.Database.CanConnectAsync())
            {
                _logger.LogInformation($"Database for tenant {tenant.Name} does not exist. Creating and applying migrations...");
                await context.Database.MigrateAsync();
            }
            else
            {
                _logger.LogInformation($"Database for tenant {tenant.Name} exists. Checking for and applying any pending migrations...");
                if (!await context.Database.HasMigrationsAppliedAsync())
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    _logger.LogInformation($"No pending migrations for tenant {tenant.Name}");
                }
            }

            _logger.LogInformation($"Database migration completed for tenant: {tenant.Name}");
        }

        public async Task MigrateAllTenantDatabasesAsync()
        {
            var tenants = await GetAllTenantsAsync();
            foreach (var tenant in tenants)
            {
                await MigrateTenantDatabaseAsync(tenant.Identifier);
            }
        }

        public TenantDbContext CreateTenantDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new TenantDbContext(optionsBuilder.Options);
        }
    }

    public static class DbContextExtensions
    {
        public static async Task<bool> HasMigrationsAppliedAsync(this DatabaseFacade database)
        {
            var appliedMigrations = await database.GetAppliedMigrationsAsync();
            return appliedMigrations.Any();
        }
    }
}