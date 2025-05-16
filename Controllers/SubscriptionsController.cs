using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Subscriptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Voia.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Solo para Admins
        [HttpGet]
        [HasPermission("CanViewSubscriptions")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptions()
        {
            var subscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Plan)
                .Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User.Name,
                    UserEmail = s.User.Email,
                    PlanId = s.PlanId,
                    PlanName = s.Plan.Name,
                    StartedAt = s.StartedAt,
                    ExpiresAt = s.ExpiresAt,
                    Status = s.Status
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        // Público: cualquier usuario autenticado puede suscribirse
        [HttpPost("subscribe")]
        public async Task<ActionResult<Subscription>> Subscribe([FromBody] CreateSubscriptionDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Evitar múltiples suscripciones activas del mismo usuario, si aplica
            var existing = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (existing != null)
            {
                return BadRequest(new { message = "Ya tienes una suscripción activa." });
            }

            var subscription = new Subscription
            {
                UserId = userId,
                PlanId = dto.PlanId,
                StartedAt = dto.StartedAt,
                ExpiresAt = dto.ExpiresAt,
                Status = "active"
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubscriptions), new { id = subscription.Id }, subscription);
        }
        // GET: api/subscriptions/me
        [HttpGet("me")]
        public async Task<ActionResult<SubscriptionDto>> GetMySubscription()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.Status == "active")
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User.Name,
                    UserEmail = s.User.Email,
                    PlanId = s.PlanId,
                    PlanName = s.Plan.Name,
                    StartedAt = s.StartedAt,
                    ExpiresAt = s.ExpiresAt,
                    Status = s.Status
                })
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                return NotFound(new { message = "No tienes una suscripción activa." });
            }

            return Ok(subscription);
        }
        // PUT: /api/plans/change
        [HttpPut("change")]
        public async Task<ActionResult> ChangeSubscription([FromBody] ChangePlanDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var existing = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (existing == null)
            {
                return NotFound(new { message = "No tienes una suscripción activa para cambiar." });
            }

            // Opcional: Marcar la suscripción anterior como cancelada (o expired)
            existing.Status = "canceled";
            _context.Subscriptions.Update(existing);

            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanId = dto.PlanId,
                StartedAt = dto.StartedAt,
                ExpiresAt = dto.ExpiresAt,
                Status = "active"
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Plan actualizado con éxito." });
        }


        // Solo para Admins
        [HttpPut("{id}")]
        [HasPermission("CanUpdateSubscriptions")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto updateDto)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { message = "Subscription not found." });

            if (updateDto.UserId.HasValue)
                subscription.UserId = updateDto.UserId.Value;

            if (updateDto.PlanId.HasValue)
                subscription.PlanId = updateDto.PlanId.Value;

            if (updateDto.StartedAt.HasValue)
                subscription.StartedAt = updateDto.StartedAt.Value;

            if (updateDto.ExpiresAt.HasValue)
                subscription.ExpiresAt = updateDto.ExpiresAt.Value;

            if (!string.IsNullOrEmpty(updateDto.Status))
                subscription.Status = updateDto.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Solo para Admins
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteSubscriptions")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { message = "Subscription not found." });

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
