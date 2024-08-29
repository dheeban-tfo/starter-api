using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using starterapi.Services;

namespace starterapi.Middleware
{
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantDbContext dbContext, IAuditLogService auditLogService)
        {
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var action = $"{context.Request.Method} {context.Request.Path}";
                var entityChanges = GetEntityChanges(dbContext);

                await auditLogService.LogAsync(userId, action, "Multiple", null, null, JsonConvert.SerializeObject(entityChanges));

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private static List<EntityChange> GetEntityChanges(TenantDbContext dbContext)
        {
            var changes = new List<EntityChange>();

            foreach (var entry in dbContext.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified || entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                {
                    var entityName = entry.Entity.GetType().Name;
                    var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();
                    var change = new EntityChange
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        State = entry.State.ToString(),
                        Changes = new Dictionary<string, object>()
                    };

                    foreach (var property in entry.Properties)
                    {
                        if (entry.State == EntityState.Added || property.IsModified)
                        {
                            change.Changes[property.Metadata.Name] = property.CurrentValue;
                        }
                    }

                    changes.Add(change);
                }
            }

            return changes;
        }
    }

    public class EntityChange
    {
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string State { get; set; }
        public Dictionary<string, object> Changes { get; set; }
    }
}