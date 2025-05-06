using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Subscriptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las suscripciones.
        /// </summary>
        /// <returns>Lista de suscripciones.</returns>
        /// <response code="200">Devuelve la lista de suscripciones.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewSubscriptions")]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
        {
            var subscriptions = await _context.Subscriptions.ToListAsync();
            return Ok(subscriptions);
        }

        /// <summary>
        /// Crea una nueva suscripción.
        /// </summary>
        /// <param name="dto">Datos de la nueva suscripción.</param>
        /// <returns>La suscripción recién creada.</returns>
        /// <response code="201">La suscripción fue creada exitosamente.</response>
        /// <response code="400">Si los datos de la suscripción no son válidos.</response>
        [HttpPost]
        [HasPermission("CanCreateSubscriptions")]
        public async Task<ActionResult<Subscription>> CreateSubscription([FromBody] CreateSubscriptionDto dto)
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

        /// <summary>
        /// Actualiza una suscripción existente.
        /// </summary>
        /// <param name="id">ID de la suscripción a actualizar.</param>
        /// <param name="updateSubscriptionDto">Datos de la suscripción actualizada.</param>
        /// <returns>Respuesta de la operación de actualización.</returns>
        /// <response code="200">La suscripción fue actualizada correctamente.</response>
        /// <response code="400">Si los datos de la suscripción son inválidos.</response>
        /// <response code="404">Si no se encuentra la suscripción a actualizar.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateSubscriptions")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto updateSubscriptionDto)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return NotFound(new { message = "Subscription not found." });
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

        /// <summary>
        /// Elimina una suscripción por su ID.
        /// </summary>
        /// <param name="id">ID de la suscripción a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">La suscripción fue eliminada correctamente.</response>
        /// <response code="404">Si no se encuentra la suscripción a eliminar.</response>
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
