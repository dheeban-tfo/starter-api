using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly TenantDbContext _context;

        public AuditLogController(TenantDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string userId, [FromQuery] string action, [FromQuery] string entityName)
        {
            IQueryable<AuditLog> query = _context.AuditLogs;

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
            return Ok(logs);
        }
    }
}