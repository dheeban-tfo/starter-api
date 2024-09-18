using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace starterapi.Services
{
    public class TenantDbSchemaUpdater
    {
        private readonly ILogger<TenantDbSchemaUpdater> _logger;

        public TenantDbSchemaUpdater(ILogger<TenantDbSchemaUpdater> logger)
        {
            _logger = logger;
        }

        public async Task UpdateSchemaAsync(TenantDbContext context)
        {
            var dbName = context.Database.GetDbConnection().Database;
            _logger.LogInformation($"Updating schema for database: {dbName}");

            try
            {
                await context.Database.EnsureCreatedAsync();

                // Check and update schema version
                await UpdateSchemaVersion(context);

                _logger.LogInformation($"Schema update completed for {dbName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating schema for {dbName}");
                throw;
            }
        }

        private async Task UpdateSchemaVersion(TenantDbContext context)
        {
            if (!await TableExistsAsync(context, "SchemaVersions"))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE SchemaVersions (Version INT NOT NULL PRIMARY KEY)");
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO SchemaVersions (Version) VALUES (1)");
            }

            var currentVersion = await GetCurrentSchemaVersionAsync(context);

            if (currentVersion < 2)
            {
                await ApplySchemaUpdateToV2(context);
            }

            // Add more version checks and update methods as needed
        }

        private async Task<int> GetCurrentSchemaVersionAsync(TenantDbContext context)
        {
            var result = await context.Database.SqlQuery<int>($"SELECT TOP 1 Version FROM SchemaVersions ORDER BY Version DESC").ToListAsync();
            return result.FirstOrDefault();
        }

        private async Task ApplySchemaUpdateToV2(TenantDbContext context)
        {
            // Apply schema changes for version 2
            // For example:
            if (!await ColumnExistsAsync(context, "Users", "LastLoginDate"))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE Users ADD LastLoginDate DATETIME NULL");
            }

            // Update schema version
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE SchemaVersions SET Version = 2 WHERE Version = 1");
        }

        private async Task<bool> TableExistsAsync(TenantDbContext context, string tableName)
        {
            var conn = context.Database.GetDbConnection();
            try
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') THEN 1 ELSE 0 END";
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }

        private async Task<bool> ColumnExistsAsync(TenantDbContext context, string tableName, string columnName)
        {
            var conn = context.Database.GetDbConnection();
            try
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}') THEN 1 ELSE 0 END";
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }
    }
}