using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace starterapi.Services
{
    public interface IMigrationService
    {
        Task<string> MigrateSpecificTenantAsync(string tenantId);
        Task<string> MigrateAllTenantsAsync();
        Task<string> SeedTenantDataAsync(string tenantId);
    }

    public class MigrationService : IMigrationService
    {
        private readonly TenantManagementDbContext _tenantManagementDbContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MigrationService> _logger;

        public MigrationService(
            TenantManagementDbContext tenantManagementDbContext,
            IServiceProvider serviceProvider,
            ILogger<MigrationService> logger
        )
        {
            _tenantManagementDbContext = tenantManagementDbContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<string> MigrateSpecificTenantAsync(string tenantId)
        {
            var tenant = await _tenantManagementDbContext.Tenants.FirstOrDefaultAsync(t =>
                t.Identifier == tenantId
            );
            if (tenant == null)
            {
                throw new ArgumentException($"Tenant with ID {tenantId} not found.");
            }

            var result = await MigrateTenantDatabaseAsync(tenant.ConnectionString, tenantId);
            await SeedTenantDataAsync(tenantId);
            return result;
        }

        public async Task<string> MigrateAllTenantsAsync()
        {
            var tenants = await _tenantManagementDbContext.Tenants.ToListAsync();
            var results = new List<string>();
            foreach (var tenant in tenants)
            {
                var migrationResult = await MigrateTenantDatabaseAsync(
                    tenant.ConnectionString,
                    tenant.Identifier
                );
                var seedingResult = await SeedTenantDataAsync(tenant.Identifier);
                results.Add($"{migrationResult}\n{seedingResult}");
            }
            return string.Join("\n\n", results);
        }

        private async Task<string> MigrateTenantDatabaseAsync(
            string connectionString,
            string tenantId
        )
        {
            _logger.LogInformation($"Attempting to migrate database for tenant {tenantId}");

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            using var context = new TenantDbContext(optionsBuilder.Options);

            try
            {
                // Check if the database exists
                bool dbExists = await context.Database.CanConnectAsync();

                if (!dbExists)
                {
                    // If the database doesn't exist, create it and apply migrations
                    await context.Database.MigrateAsync();
                    _logger.LogInformation(
                        $"Created and migrated new database for tenant {tenantId}"
                    );
                }
                else
                {
                    // If the database exists, check if it has any migrations applied
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

                    if (!appliedMigrations.Any())
                    {
                        // If no migrations are applied, but the database exists (created by EnsureCreated),
                        // we need to mark the initial migration as applied without running it
                        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                        if (pendingMigrations.Any())
                        {
                            var initialMigration = pendingMigrations.First();
                            await context.Database.ExecuteSqlRawAsync(
                                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{initialMigration}', '{context.GetType().Assembly.GetName().Version}')"
                            );

                            _logger.LogInformation(
                                $"Marked initial migration as applied for tenant {tenantId}"
                            );
                        }
                    }

                    // Apply any pending migrations
                    await context.Database.MigrateAsync();
                }

                _logger.LogInformation($"Successfully migrated database for tenant {tenantId}");
                return $"Successfully migrated database for tenant {tenantId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating database for tenant {tenantId}");
                return $"Error migrating database for tenant {tenantId}: {ex.Message}";
            }

            // _logger.LogInformation($"Attempting to migrate database for tenant {tenantId}");

            // var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            // optionsBuilder.UseSqlServer(connectionString);

            // using var context = new TenantDbContext(optionsBuilder.Options);

            // try
            // {
            //     // Ensure database exists
            //     await context.Database.EnsureCreatedAsync();

            //     // Apply migrations
            //     await context.Database.MigrateAsync();

            //     _logger.LogInformation($"Successfully migrated database for tenant {tenantId}");
            //     return $"Successfully migrated database for tenant {tenantId}";
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, $"Error migrating database for tenant {tenantId}");
            //     return $"Error migrating database for tenant {tenantId}: {ex.Message}";
            // }

            //--------
            // _logger.LogInformation($"Attempting to migrate database for tenant {tenantId}");

            // var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            // optionsBuilder.UseSqlServer(connectionString);

            // using var context = new TenantDbContext(optionsBuilder.Options);

            // try
            // {
            //     // Check if the database exists, if not, create it
            //     await context.Database.EnsureCreatedAsync();

            //     // Check if __EFMigrationsHistory table exists
            //     var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            //     bool migrationTableExists = appliedMigrations.Any();

            //     if (!migrationTableExists)
            //     {
            //         _logger.LogInformation(
            //             $"__EFMigrationsHistory table not found for tenant {tenantId}. Creating initial migration."
            //         );
            //         await context.Database.MigrateAsync();
            //         return $"Created initial migration for tenant {tenantId}";
            //     }

            //     var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            //     var pendingMigrationsList = pendingMigrations.ToList();

            //     if (!pendingMigrationsList.Any())
            //     {
            //         _logger.LogInformation($"No pending migrations for tenant {tenantId}");
            //         return $"No pending migrations for tenant {tenantId}";
            //     }

            //     _logger.LogInformation(
            //         $"Pending migrations for tenant {tenantId}: {string.Join(", ", pendingMigrationsList)}"
            //     );

            //     await context.Database.MigrateAsync();
            //     _logger.LogInformation($"Successfully applied migrations for tenant {tenantId}");
            //     return $"Successfully applied migrations for tenant {tenantId}: {string.Join(", ", pendingMigrationsList)}";
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, $"Error applying migrations for tenant {tenantId}");
            //     return $"Error applying migrations for tenant {tenantId}: {ex.Message}";
            // }
        }

        public async Task<string> SeedTenantDataAsync(string tenantId)
        {
            try
            {
                await TenantSeeder.SeedTenantAsync(_serviceProvider, tenantId);
                return $"Successfully seeded data for tenant {tenantId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding data for tenant {tenantId}");
                return $"Error seeding data for tenant {tenantId}: {ex.Message}";
            }
        }
    }
}
