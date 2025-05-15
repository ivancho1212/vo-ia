using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Voia.Api.Data;
using Voia.Api.Models.Plans;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los planes disponibles.
        /// </summary>
        /// <returns>Lista de planes disponibles.</returns>
        /// <response code="200">Devuelve la lista de todos los planes.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [AllowAnonymous] // Permite el acceso público a este método
        public async Task<ActionResult<IEnumerable<Plan>>> GetPlans()
        {
            var plans = await _context.Plans.ToListAsync();
            return Ok(plans);
        }
        /// <summary>
        /// Obtiene el plan asociado a la suscripción del usuario autenticado.
        /// </summary>
        /// <returns>Información del plan actual del usuario.</returns>
        /// <response code="200">Devuelve el plan actual del usuario.</response>
        /// <response code="404">Si no se encuentra la suscripción activa.</response>
        [HttpGet("my-plan")]
        [Authorize] // Cualquier usuario autenticado
        public async Task<IActionResult> GetMyPlan()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario." });

            var userId = userIdClaim.Value;

            // Buscar la suscripción activa del usuario
            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s =>
                    s.UserId == int.Parse(userId) &&
                    s.Status == "active" &&
                    s.ExpiresAt > DateTime.UtcNow);

            if (subscription == null)
                return NotFound(new { message = "No se encontró una suscripción activa para este usuario." });

            var plan = subscription.Plan;

            return Ok(new
            {
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price,
                plan.MaxTokens,
                plan.BotsLimit,
                plan.IsActive,
                subscription.StartedAt,
                subscription.ExpiresAt
            });
        }

        /// <summary>
        /// Crea un nuevo plan.
        /// </summary>
        /// <param name="plan">Datos del nuevo plan.</param>
        /// <returns>El plan recién creado.</returns>
        /// <response code="201">El plan fue creado exitosamente.</response>
        /// <response code="400">Si los datos del plan son inválidos.</response>
        /// <response code="409">Si ya existe un plan con el mismo nombre.</response>
        [HttpPost]
        [HasPermission("CanCreatePlans")]
        public async Task<ActionResult<Plan>> CreatePlan([FromBody] Plan plan)
        {
            // Validar nombre único
            if (await _context.Plans.AnyAsync(p => p.Name == plan.Name))
                return Conflict(new { message = "Ya existe un plan con ese nombre." });

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, plan);
        }

        /// <summary>
        /// Actualiza un plan existente.
        /// </summary>
        /// <param name="id">ID del plan a actualizar.</param>
        /// <param name="plan">Datos actualizados del plan.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">El plan fue actualizado correctamente.</response>
        /// <response code="400">Si los datos son inválidos.</response>
        /// <response code="404">Si no se encuentra el plan.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdatePlans")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] Plan plan)
        {
            if (id != plan.Id)
                return BadRequest(new { message = "El ID del plan no coincide." });

            var existingPlan = await _context.Plans.FindAsync(id);
            if (existingPlan == null)
                return NotFound(new { message = "Plan no encontrado." });

            existingPlan.Name = plan.Name;
            existingPlan.Description = plan.Description;
            existingPlan.Price = plan.Price;
            existingPlan.MaxTokens = plan.MaxTokens;
            existingPlan.BotsLimit = plan.BotsLimit;
            existingPlan.IsActive = plan.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Elimina un plan por su ID.
        /// </summary>
        /// <param name="id">ID del plan a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">El plan fue eliminado correctamente.</response>
        /// <response code="404">Si no se encuentra el plan.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeletePlans")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plan no encontrado." });

            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
