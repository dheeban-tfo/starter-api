using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;

namespace starterapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(ITenantDbContextAccessor contextAccessor, ILogger<AuditLogController> logger)
        {
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string userId, [FromQuery] string action, [FromQuery] string entityName)
        {
            _logger.LogInformation("Retrieving audit logs with filters: startDate={StartDate}, endDate={EndDate}, userId={UserId}, action={Action}, entityName={EntityName}", 
                startDate, endDate, userId, action, entityName);

            var context = _contextAccessor.TenantDbContext;
            IQueryable<AuditLog> query = context.AuditLogs;

            if (startDate.HasValue)
                query = query.Where(log => log.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(log => log.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(log => log.Action == action);

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(log => log.EntityName == entityName);

            var logs = await query.OrderByDescending(log => log.Timestamp).ToListAsync();

            _logger.LogInformation("Retrieved {LogCount} audit logs", logs.Count);

            return Ok(logs);
        }
    }
}