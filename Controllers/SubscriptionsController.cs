using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Subscriptions;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            var subscription = new Subscription
            {
                UserId = dto.UserId,
                PlanId = dto.PlanId,
                StartedAt = dto.StartedAt,
                ExpiresAt = dto.ExpiresAt,
                Status = dto.Status
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubscriptions), new { id = subscription.Id }, subscription);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto updateSubscriptionDto)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // Solo actualizamos los campos que no sean nulos
            if (updateSubscriptionDto.UserId.HasValue)
            {
                subscription.UserId = updateSubscriptionDto.UserId.Value;
            }

            if (updateSubscriptionDto.PlanId.HasValue)
            {
                subscription.PlanId = updateSubscriptionDto.PlanId.Value;
            }

            if (updateSubscriptionDto.StartedAt.HasValue)
            {
                subscription.StartedAt = updateSubscriptionDto.StartedAt.Value;
            }

            if (updateSubscriptionDto.ExpiresAt.HasValue)
            {
                subscription.ExpiresAt = updateSubscriptionDto.ExpiresAt.Value;
            }

            if (!string.IsNullOrEmpty(updateSubscriptionDto.Status))
            {
                subscription.Status = updateSubscriptionDto.Status;
            }

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound();

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
