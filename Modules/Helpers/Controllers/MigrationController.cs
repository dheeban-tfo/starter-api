using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi;
using starterapi.Services;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly TenantManagementDbContext _tenantManagementContext;
        private readonly IMigrationService _migrationService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            TenantManagementDbContext tenantManagementContext,
            ILogger<MigrationController> logger,
            IMigrationService migrationService
        )
        {
            _tenantManagementContext = tenantManagementContext;
            _logger = logger;
            _migrationService = migrationService;
        }

        [HttpPost("apply-migrations")]
        public async Task<IActionResult> ApplyMigrations()
        {
            var tenants = await _tenantManagementContext.Tenants.ToListAsync();
            foreach (var tenant in tenants)
            {
                await ApplyTenantMigrations(tenant.ConnectionString);
            }
            return Ok("Migrations applied to all tenant databases.");
        }

        [HttpPost("apply-all")]
        public async Task<IActionResult> ApplyAllMigrations()
        {
            var result = await _migrationService.MigrateAllTenantsAsync();
            return Ok(result);
        }

        private async Task ApplyTenantMigrations(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            using var context = new TenantDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync();
            _logger.LogInformation($"Migrations applied for tenant database: {connectionString}");
        }

        [HttpPost("seed-tenant/{tenantId}")]
        public async Task<IActionResult> SeedTenantData(string tenantId)
        {
            var result = await _migrationService.SeedTenantDataAsync(tenantId);
            return Ok(result);
        }
    }
}
