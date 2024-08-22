using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string action, string entityName, string entityId, string oldValues, string newValues);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? startDate, DateTime? endDate, string userId, string action, string entityName);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AuditLogService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task LogAsync(string userId, string action, string entityName, string entityId, string oldValues, string newValues)
        {
            using var context = _contextFactory.CreateDbContext();
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? startDate, DateTime? endDate, string userId, string action, string entityName)
        {
            using var context = _contextFactory.CreateDbContext();
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

            return await query.OrderByDescending(log => log.Timestamp).ToListAsync();
        }
    }
}