using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using starterapi.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace starterapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Root,Admin")] 
    public class TenantController : ControllerBase
    {
        private readonly TenantManagementDbContext _context;
        private readonly ILogger<TenantController> _logger;

        public TenantController(TenantManagementDbContext context, ILogger<TenantController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateTenants")]
        public async Task<ActionResult<Tenant>> CreateTenant(Tenant tenant, [FromHeader(Name = "X-TenantId")] string tenantId)
        {
            if (!await IsUserAuthorized(tenantId))
            {
                return Forbid();
            }

            _logger.LogInformation("Creating new tenant: {TenantName}", tenant.Name);
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Initialize the new tenant's database
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(tenant.ConnectionString);
            using var tenantDbContext = new TenantDbContext(optionsBuilder.Options);
            await tenantDbContext.Database.MigrateAsync();
            DbSeeder.SeedTenantData(tenantDbContext);

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants([FromHeader(Name = "X-TenantId")] string tenantId)
        {
            if (!await IsUserAuthorized(tenantId))
            {
                return Forbid();
            }

            _logger.LogInformation("Retrieving all tenants");
            return await _context.Tenants.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant(int id, [FromHeader(Name = "X-TenantId")] string tenantId)
        {
            if (!await IsUserAuthorized(tenantId))
            {
                return Forbid();
            }

            _logger.LogInformation("Retrieving tenant with ID: {TenantId}", id);
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found with ID: {TenantId}", id);
                return NotFound();
            }

            return tenant;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(int id, Tenant tenant, [FromHeader(Name = "X-TenantId")] string tenantId)
        {
            if (!await IsUserAuthorized(tenantId))
            {
                return Forbid();
            }

            if (id != tenant.Id)
            {
                return BadRequest();
            }

            _context.Entry(tenant).State = EntityState.Modified;

            try
            {
                _logger.LogInformation("Updating tenant with ID: {TenantId}", id);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TenantExists(id))
                {
                    _logger.LogWarning("Tenant not found with ID: {TenantId}", id);
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTenant(int id, [FromHeader(Name = "X-TenantId")] string tenantId)
        {
            if (!await IsUserAuthorized(tenantId))
            {
                return Forbid();
            }

            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found with ID: {TenantId}", id);
                return NotFound();
            }

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Deactivating tenant with ID: {TenantId}", id);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TenantExists(int id)
        {
            return _context.Tenants.Any(e => e.Id == id);
        }

        private async Task<bool> IsUserAuthorized(string tenantId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == int.Parse(userId))
                .Include(ur => ur.Role)
                .ToListAsync();

            var isAuthorized = userRoles.Any(ur => ur.Role.Name == "Root" || ur.Role.Name == "Admin");

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantId);
            var isTenantValid = tenant != null && tenant.IsActive;

            return isAuthorized && isTenantValid;
        }

        
        [HttpPost]
        private async Task ApplyTenantMigrations(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            using var context = new TenantDbContext(optionsBuilder.Options);

            // Check if the database exists
            if (!await context.Database.CanConnectAsync())
            {
                _logger.LogInformation(
                    "Database does not exist. Creating and applying migrations..."
                );
                await context.Database.MigrateAsync();
            }
            else
            {
                _logger.LogInformation("Database exists. Applying any pending migrations...");
                await context.Database.MigrateAsync();
            }

            _logger.LogInformation("Migrations applied successfully.");
        }
    }
}