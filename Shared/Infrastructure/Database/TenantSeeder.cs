using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using starterapi.Models;

namespace starterapi;

public static class TenantSeeder
{
    public static async Task SeedTenantAsync(IServiceProvider serviceProvider, string tenantId)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantManagementContext =
            scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var tenant = await tenantManagementContext.Tenants.FirstOrDefaultAsync(t =>
                t.Identifier == tenantId
            );
            if (tenant == null)
            {
                logger.LogWarning($"Tenant with identifier {tenantId} not found.");
                return;
            }

            logger.LogInformation($"Seeding data for tenant: {tenant.Name}");

            await SeedTenantDataAsync(tenant.ConnectionString, logger);

            logger.LogInformation($"Completed seeding data for tenant: {tenant.Name}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                $"An error occurred while seeding the database for tenant {tenantId}."
            );
            throw;
        }
    }

    private static async Task SeedTenantDataAsync(string connectionString, ILogger logger)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new TenantDbContext(optionsBuilder.Options);

        // Ensure the tenant database is created
        context.Database.EnsureCreated();

        // Seed the tenant data
        DbSeeder.SeedTenantData(context);

        logger.LogInformation($"Completed seeding tenant data for connection: {connectionString}");
    }

    public static void SeedTenants(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantManagementContext =
            scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            EnsureHangfireDatabaseCreated(configuration, logger);

            // Apply migrations to the tenant management database
            tenantManagementContext.Database.Migrate();

            // Seed roles and root admin
            SeedRolesAndRootAdmin(tenantManagementContext, logger);

            logger.LogInformation("Ensuring tenant databases are created and seeded...");

            var tenants = tenantManagementContext.Tenants.ToList();
            if (tenants.Count == 0)
            {
                logger.LogInformation("No tenants found. Creating default tenants...");

                tenants.Add(
                    new Tenant
                    {
                        Name = "Alpha",
                        Identifier = "alpha",
                        ConnectionString =
                            "Server=localhost;Database=AlphaTenantDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=true"
                    }
                );

                tenants.Add(
                    new Tenant
                    {
                        Name = "Beta",
                        Identifier = "beta",
                        ConnectionString =
                            "Server=localhost;Database=BetaTenantDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=true"
                    }
                );

                tenantManagementContext.Tenants.AddRange(tenants);
                tenantManagementContext.SaveChanges();
            }

            foreach (var tenant in tenants)
            {
                CreateAndSeedTenantData(tenant.ConnectionString, logger);
            }

            logger.LogInformation("Tenant database creation and seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during tenant seeding.");
            throw;
        }
    }

    private static void CreateAndSeedTenantData(string connectionString, ILogger logger)
    {
        logger.LogInformation(
            $"Creating and seeding tenant data for connection: {connectionString}"
        );

        // First, ensure the database exists
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using (var connection = new SqlConnection(builder.ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
                $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";

            logger.LogInformation(
                $"Executing SQL command to create tenant database: {command.CommandText}"
            );
            command.ExecuteNonQuery();
            logger.LogInformation($"Tenant database '{databaseName}' created or already exists.");
        }

        // Now, use EF Core to ensure the schema is created
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new TenantDbContext(optionsBuilder.Options);

        // Create the schema
        context.Database.EnsureCreated();

        // Now seed the tenant data
        DbSeeder.SeedTenantData(context);

        logger.LogInformation(
            $"Completed creating and seeding tenant data for connection: {connectionString}"
        );
    }

    private static void SeedRolesAndRootAdmin(TenantManagementDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding roles and root admin...");

        // Seed roles if they don't exist
        if (!context.Roles.Any())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Root" },
                new Role { Name = "Admin" }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();
        }

        // Seed root admin if it doesn't exist
        if (!context.Users.Any(u => u.Email == "rootadmin@example.com"))
        {
            var rootRole = context.Roles.FirstOrDefault(r => r.Name == "Root");
            if (rootRole == null)
            {
                logger.LogError("Root role not found. Cannot create root admin.");
                return;
            }

            var rootAdmin = new User
            {
                FirstName = "Root",
                LastName = "Admin",
                Email = "rootadmin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RootAdminPassword123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(rootAdmin);
            context.SaveChanges();

            var userRole = new UserRole { UserId = rootAdmin.Id, RoleId = rootRole.Id };

            context.UserRoles.Add(userRole);
            context.SaveChanges();

            logger.LogInformation("Root admin created successfully.");
        }
        else
        {
            logger.LogInformation("Root admin already exists. Skipping creation.");
        }
    }

    private static void SeedTenantData(string connectionString, ILogger logger)
    {
        logger.LogInformation($"Seeding tenant data for connection: {connectionString}");

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new TenantDbContext(optionsBuilder.Options);

        // Ensure the tenant database is created
        context.Database.EnsureCreated();

        // Seed the tenant data
        DbSeeder.SeedTenantData(context);

        logger.LogInformation($"Completed seeding tenant data for connection: {connectionString}");
    }

    private static void EnsureHangfireDatabaseCreated(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("HangfireConnection");
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using (var connection = new SqlConnection(builder.ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
                $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";

            logger.LogInformation(
                $"Executing SQL command to create Hangfire database: {command.CommandText}"
            );
            command.ExecuteNonQuery();
            logger.LogInformation($"Hangfire database '{databaseName}' created or already exists.");
        }
    }

    // private static void MigrateTenantData(string connectionString, ILogger logger)
    // {
    //     logger.LogInformation($"Migrating tenant data for connection: {connectionString}");

    //     var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
    //     optionsBuilder.UseSqlServer(connectionString);

    //     using var context = new TenantDbContext(optionsBuilder.Options);

    //     // Apply migrations to the tenant database
    //     context.Database.Migrate();

    //     // Seed the tenant data
    //     DbSeeder.SeedTenantData(context);

    //     logger.LogInformation(
    //         $"Completed migrating tenant data for connection: {connectionString}"
    //     );
    // }
}
