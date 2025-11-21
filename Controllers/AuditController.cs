using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditController> _logger;

        public AuditController(ApplicationDbContext context, ILogger<AuditController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get activity logs with optional filtering
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<ActivityLogResponseDto>> GetActivityLogs(
            [FromQuery] int? userId = null,
            [FromQuery] string entityType = null,
            [FromQuery] string action = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 1;
                if (pageSize > 100) pageSize = 100; // Max 100 items per page

                var query = _context.ActivityLogs.AsQueryable();

                // Apply filters
                if (userId.HasValue)
                {
                    query = query.Where(a => a.UserId == userId);
                }

                if (!string.IsNullOrEmpty(entityType))
                {
                    query = query.Where(a => a.EntityType == entityType);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action == action);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= startDate);
                }

                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1);
                    query = query.Where(a => a.CreatedAt < endOfDay);
                }

                var total = await query.CountAsync();

                var logs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        OldValues = a.OldValues,
                        NewValues = a.NewValues,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent,
                        RequestId = a.RequestId,
                        Description = a.Description,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {logs.Count} activity logs for page {pageNumber}");

                return Ok(new ActivityLogResponseDto
                {
                    Data = logs,
                    Total = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity logs");
                return StatusCode(500, new { message = "Error retrieving activity logs", error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit log for a specific entity
        /// </summary>
        [HttpGet("entity/{entityType}/{entityId}")]
        public async Task<ActionResult<List<ActivityLogDto>>> GetEntityAuditTrail(
            string entityType,
            int entityId)
        {
            try
            {
                if (string.IsNullOrEmpty(entityType))
                {
                    return BadRequest("Entity type is required");
                }

                var logs = await _context.ActivityLogs
                    .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        OldValues = a.OldValues,
                        NewValues = a.NewValues,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent,
                        RequestId = a.RequestId,
                        Description = a.Description,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {logs.Count} audit logs for {entityType} #{entityId}");

                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving audit trail for {entityType} #{entityId}");
                return StatusCode(500, new { message = "Error retrieving audit trail", error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetAuditStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var query = _context.ActivityLogs.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= startDate);
                }

                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1);
                    query = query.Where(a => a.CreatedAt < endOfDay);
                }

                var stats = new
                {
                    TotalActivities = await query.CountAsync(),
                    CreateCount = await query.Where(a => a.Action == "CREATE").CountAsync(),
                    UpdateCount = await query.Where(a => a.Action == "UPDATE").CountAsync(),
                    DeleteCount = await query.Where(a => a.Action == "DELETE").CountAsync(),
                    ActivitiesByEntity = await query
                        .GroupBy(a => a.EntityType)
                        .Select(g => new { EntityType = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    ActivitiesByUser = await query
                        .Where(a => a.UserId.HasValue)
                        .GroupBy(a => a.UserId)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    ActivitiesByDate = await query
                        .GroupBy(a => a.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .OrderByDescending(g => g.Date)
                        .Take(30) // Last 30 days
                        .ToListAsync()
                };

                _logger.LogInformation("Retrieved audit statistics");

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit statistics");
                return StatusCode(500, new { message = "Error retrieving audit statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activity log by ID
        /// </summary>
        [HttpGet("logs/{id}")]
        public async Task<ActionResult<ActivityLogDto>> GetActivityLogById(int id)
        {
            try
            {
                var log = await _context.ActivityLogs
                    .Where(a => a.Id == id)
                    .Select(a => new ActivityLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        OldValues = a.OldValues,
                        NewValues = a.NewValues,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent,
                        RequestId = a.RequestId,
                        Description = a.Description,
                        CreatedAt = a.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (log == null)
                {
                    return NotFound("Activity log not found");
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving activity log {id}");
                return StatusCode(500, new { message = "Error retrieving activity log", error = ex.Message });
            }
        }
    }
}
