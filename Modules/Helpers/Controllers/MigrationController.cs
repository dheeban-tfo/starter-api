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
            try
            {
                var tenants = await _tenantManagementContext.Tenants.ToListAsync();
                var results = new List<string>();

                foreach (var tenant in tenants)
                {
                    var result = await _migrationService.MigrateSpecificTenantAsync(
                        tenant.Identifier
                    );
                    results.Add(result);
                }

                return Ok(string.Join("\n", results));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying migrations to all tenant databases");
                return StatusCode(
                    500,
                    "An error occurred while applying migrations to tenant databases"
                );
            }
        }

        [HttpPost("apply-all")]
        public async Task<IActionResult> ApplyAllMigrations()
        {
            try
            {
                var result = await _migrationService.MigrateAllTenantsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying migrations and seeding all tenant databases");
                return StatusCode(
                    500,
                    "An error occurred while applying migrations and seeding tenant databases"
                );
            }
        }

        
        

        [HttpPost("seed-tenant/{tenantId}")]
        public async Task<IActionResult> SeedTenantData(string tenantId)
        {
            try
            {
                var result = await _migrationService.SeedTenantDataAsync(tenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding data for tenant {tenantId}");
                return StatusCode(
                    500,
                    $"An error occurred while seeding data for tenant {tenantId}"
                );
            }
        }
    }
}
