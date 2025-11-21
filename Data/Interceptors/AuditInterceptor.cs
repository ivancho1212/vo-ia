using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Voia.Api.Models;

namespace Voia.Api.Data.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is ApplicationDbContext context)
            {
                var userId = GetCurrentUserId();
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();
                var requestId = GetRequestId();

                var changedEntries = context.ChangeTracker
                    .Entries()
                    .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                    .ToList();

                var auditLogs = new List<ActivityLog>();

                foreach (var entry in changedEntries)
                {
                    var entityName = entry.Entity.GetType().Name;
                    var entityId = GetEntityId(entry);

                    if (entry.State == EntityState.Added)
                    {
                        var newValues = GetNewValues(entry);
                        auditLogs.Add(new ActivityLog
                        {
                            UserId = userId,
                            Action = "CREATE",
                            EntityType = entityName,
                            EntityId = entityId,
                            OldValues = string.Empty,
                            NewValues = JsonConvert.SerializeObject(newValues),
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            RequestId = requestId,
                            Description = $"Created new {entityName}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        var oldValues = GetOldValues(entry);
                        var newValues = GetNewValues(entry);

                        // Solo crear log si hay cambios reales
                        if (oldValues.Any() || newValues.Any())
                        {
                            auditLogs.Add(new ActivityLog
                            {
                                UserId = userId,
                                Action = "UPDATE",
                                EntityType = entityName,
                                EntityId = entityId,
                                OldValues = JsonConvert.SerializeObject(oldValues),
                                NewValues = JsonConvert.SerializeObject(newValues),
                                IpAddress = ipAddress,
                                UserAgent = userAgent,
                                RequestId = requestId,
                                Description = $"Updated {entityName}",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        var oldValues = GetOldValues(entry);
                        auditLogs.Add(new ActivityLog
                        {
                            UserId = userId,
                            Action = "DELETE",
                            EntityType = entityName,
                            EntityId = entityId,
                            OldValues = JsonConvert.SerializeObject(oldValues),
                            NewValues = string.Empty,
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            RequestId = requestId,
                            Description = $"Deleted {entityName}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (auditLogs.Any())
                {
                    context.ActivityLogs.AddRange(auditLogs);
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private int? GetCurrentUserId()
        {
            try
            {
                var userIdClaim = _httpContextAccessor?.HttpContext?.User
                    ?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                    ?.Value;

                if (int.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }
            catch { }

            return null;
        }

        private string? GetClientIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    {
                        return httpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',').First().Trim();
                    }

                    return httpContext.Connection?.RemoteIpAddress?.ToString() ?? null;
                }
            }
            catch { }

            return null;
        }

        private string? GetUserAgent()
        {
            try
            {
                return _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? null;
            }
            catch { }

            return null;
        }

        private string? GetRequestId()
        {
            try
            {
                return _httpContextAccessor?.HttpContext?.TraceIdentifier ?? null;
            }
            catch { }

            return null;
        }

        private int? GetEntityId(EntityEntry entry)
        {
            try
            {
                var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                    .Select(p => entry.CurrentValues[p])
                    .FirstOrDefault();

                if (keyValues is int intId)
                {
                    return intId;
                }
            }
            catch { }

            return null;
        }

        private Dictionary<string, object> GetOldValues(EntityEntry entry)
        {
            var oldValues = new Dictionary<string, object>();

            foreach (var property in entry.Properties)
            {
                if (property.IsModified || entry.State == EntityState.Deleted)
                {
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                }
            }

            return oldValues;
        }

        private Dictionary<string, object> GetNewValues(EntityEntry entry)
        {
            var newValues = new Dictionary<string, object>();

            foreach (var property in entry.Properties)
            {
                if (entry.State == EntityState.Added || property.IsModified)
                {
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
            }

            return newValues;
        }
    }
}
