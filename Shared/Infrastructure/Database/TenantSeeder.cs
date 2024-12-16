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
        var tenantManagementContext = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // First, ensure Hangfire database exists
            EnsureHangfireDatabaseCreated(configuration, logger);

            logger.LogInformation("Checking and applying pending migrations...");
            
            // Check if there are any pending migrations
            if (tenantManagementContext.Database.GetPendingMigrations().Any())
            {
                logger.LogInformation("Applying pending migrations to tenant management database...");
                tenantManagementContext.Database.Migrate();
                logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found.");
            }

            // Now seed roles and root admin
            SeedRolesAndRootAdmin(tenantManagementContext, logger);

            // Handle tenant creation and seeding
            var tenants = tenantManagementContext.Tenants.ToList();
            if (!tenants.Any())
            {
                logger.LogInformation("No tenants found. Creating default tenants...");

                tenants.AddRange(new[]
                {
                    new Tenant
                    {
                        Name = "Alpha",
                        Identifier = "alpha",
                        ConnectionString = "Server=localhost;Database=AlphaTenantDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=true"
                    },
                    new Tenant
                    {
                        Name = "Beta",
                        Identifier = "beta",
                        ConnectionString = "Server=localhost;Database=BetaTenantDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=true"
                    }
                });

                tenantManagementContext.Tenants.AddRange(tenants);
                tenantManagementContext.SaveChanges();
            }

            // Create and seed tenant databases
            foreach (var tenant in tenants)
            {
                CreateAndSeedTenantData(tenant.ConnectionString, logger);
            }

            logger.LogInformation("Tenant database creation and seeding completed successfully.");
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
        try
        {
            logger.LogInformation("Starting to seed roles and root admin...");

            // First ensure database exists and is created
            if (!context.Database.CanConnect())
            {
                logger.LogInformation("Database does not exist. Creating database...");
                context.Database.EnsureCreated();
            }

            // Then check for pending migrations
            var pendingMigrations = context.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying pending migrations...");
                context.Database.Migrate();
            }

            // Now proceed with seeding
            using var transaction = context.Database.BeginTransaction();
            try
            {
                // Seed roles if they don't exist
                if (!context.Roles.Any())
                {
                    logger.LogInformation("Seeding roles...");
                    var roles = new List<Role>
                    {
                        new Role { Name = "Root Admin" },
                        new Role { Name = "Admin" },
                        new Role { Name = "User" }
                    };

                    context.Roles.AddRange(roles);
                    context.SaveChanges();
                    logger.LogInformation("Roles seeded successfully.");
                }

                // Seed root admin if it doesn't exist
                if (!context.Users.Any(u => u.Email == "rootadmin@example.com"))
                {
                    logger.LogInformation("Creating root admin user...");
                    var rootRole = context.Roles.FirstOrDefault(r => r.Name == "Root Admin")
                        ?? throw new InvalidOperationException("Root Admin role not found");

                    var rootAdmin = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Root",
                        LastName = "Admin",
                        Email = "rootadmin@example.com",
                        PhoneNumber = null,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("RootAdminPassword123!"),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        EmailVerified = true
                    };

                    context.Users.Add(rootAdmin);
                    context.SaveChanges();

                    context.UserRoles.Add(new UserRole { UserId = rootAdmin.Id, RoleId = rootRole.Id });
                    context.SaveChanges();

                    logger.LogInformation("Root admin created successfully.");
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding roles and root admin");
            throw;
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
