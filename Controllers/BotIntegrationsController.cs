using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotIntegrations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotIntegrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotIntegrationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las integraciones de bots.
        /// </summary>
        /// <returns>Lista de integraciones de bots.</returns>
        /// <response code="200">Devuelve la lista de integraciones de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBotIntegrations")]
        public async Task<ActionResult<IEnumerable<BotIntegration>>> GetAll()
        {
            return await _context.BotIntegrations.ToListAsync();
        }

        /// <summary>
        /// Obtiene una integración de bot por su ID.
        /// </summary>
        /// <param name="id">ID de la integración de bot.</param>
        /// <returns>La integración de bot.</returns>
        /// <response code="200">Devuelve la integración de bot.</response>
        /// <response code="404">Si no se encuentra la integración de bot.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewBotIntegrations")]
        public async Task<ActionResult<BotIntegration>> GetById(int id)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound(new { message = "Bot integration not found." });

            return Ok(integration);
        }

        /// <summary>
        /// Crea una nueva integración de bot.
        /// </summary>
        /// <param name="dto">Datos para crear la integración de bot.</param>
        /// <returns>La integración de bot creada.</returns>
        /// <response code="201">Devuelve la integración de bot creada.</response>
        /// <response code="400">Si el BotId proporcionado no existe.</response>
        [HttpPost]
        [HasPermission("CanCreateBotIntegrations")]
        public async Task<ActionResult<BotIntegration>> Create([FromBody] CreateBotIntegrationDto dto)
        {
            // Verificar si el BotId existe en la base de datos
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
            {
                return BadRequest(new { message = "BotId does not exist in the system." });
            }

            var integration = new BotIntegration
            {
                BotId = dto.BotId,
                IntegrationType = dto.IntegrationType ?? "widget", // Default to "widget" if null
                AllowedDomain = dto.AllowedDomain,
                ApiToken = dto.ApiToken,
                CreatedAt = DateTime.UtcNow
            };

            _context.BotIntegrations.Add(integration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = integration.Id }, integration);
        }

        /// <summary>
        /// Actualiza una integración de bot existente.
        /// </summary>
        /// <param name="id">ID de la integración de bot que se desea actualizar.</param>
        /// <param name="dto">Datos actualizados de la integración de bot.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">Integración actualizada correctamente.</response>
        /// <response code="404">Si la integración de bot no existe.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateBotIntegrations")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBotIntegrationDto dto)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound(new { message = "Bot integration not found." });

            // Actualizar los campos de la integración si no son nulos
            if (dto.IntegrationType != null) integration.IntegrationType = dto.IntegrationType;
            if (dto.AllowedDomain != null) integration.AllowedDomain = dto.AllowedDomain;
            if (dto.ApiToken != null) integration.ApiToken = dto.ApiToken;

            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Elimina una integración de bot.
        /// </summary>
        /// <param name="id">ID de la integración de bot a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">Integración eliminada correctamente.</response>
        /// <response code="404">Si la integración de bot no existe.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteBotIntegrations")]
        public async Task<IActionResult> Delete(int id)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound(new { message = "Bot integration not found." });

            _context.BotIntegrations.Remove(integration);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
