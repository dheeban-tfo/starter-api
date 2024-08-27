﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using starterapi.Models;

namespace starterapi;

public static class TenantSeeder
{
    public static void SeedTenants(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantManagementContext = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
         var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

          EnsureHangfireDatabaseCreated(configuration, logger);

        // Ensure the tenant management database is created
        tenantManagementContext.Database.EnsureCreated();

        if (!tenantManagementContext.Tenants.Any())
        {
            logger.LogInformation("Seeding tenants...");

            // Seed root tenant
            var rootTenant = new Tenant
            {
                Name = "Root",
                Identifier = "root",
                ConnectionString = "Server=localhost;Database=RootTenantDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=true"
            };
            tenantManagementContext.Tenants.Add(rootTenant);

            // Seed alpha tenant
            var alphaTenant = new Tenant
            {
                Name = "Alpha",
                Identifier = "alpha",
                ConnectionString = "Server=localhost;Database=AlphaTenantDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=true"
            };
            tenantManagementContext.Tenants.Add(alphaTenant);

            // Seed beta tenant
            var betaTenant = new Tenant
            {
                Name = "Beta",
                Identifier = "beta",
                ConnectionString = "Server=localhost;Database=BetaTenantDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=true"
            };
            tenantManagementContext.Tenants.Add(betaTenant);

            tenantManagementContext.SaveChanges();

            // Seed data for each tenant
            SeedTenantData(alphaTenant.ConnectionString, logger);
            SeedTenantData(betaTenant.ConnectionString, logger);

            logger.LogInformation("Tenant seeding completed.");
        }
        else
        {
            logger.LogInformation("Tenants already exist. Skipping seeding process.");
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
            command.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";
            
            logger.LogInformation($"Executing SQL command to create Hangfire database: {command.CommandText}");
            command.ExecuteNonQuery();
            logger.LogInformation($"Hangfire database '{databaseName}' created or already exists.");
        }
    }
}