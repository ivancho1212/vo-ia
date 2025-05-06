using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotActions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotActionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotActionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las acciones de bots.
        /// </summary>
        /// <returns>Lista de acciones de bots.</returns>
        /// <response code="200">Devuelve la lista de acciones de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBotActions")]
        public async Task<ActionResult<IEnumerable<BotAction>>> GetAll()
        {
            return await _context.BotActions.ToListAsync();
        }

        /// <summary>
        /// Obtiene una acción de bot por su ID.
        /// </summary>
        /// <param name="id">ID de la acción de bot.</param>
        /// <returns>La acción de bot.</returns>
        /// <response code="200">Devuelve la acción de bot.</response>
        /// <response code="404">Si no se encuentra la acción de bot.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewBotActions")]
        public async Task<ActionResult<BotAction>> GetById(int id)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound(new { message = "Bot action not found." });

            return Ok(action);
        }

        /// <summary>
        /// Crea una nueva acción de bot.
        /// </summary>
        /// <param name="dto">Datos para crear la acción de bot.</param>
        /// <returns>La acción de bot creada.</returns>
        /// <response code="201">Devuelve la acción de bot creada.</response>
        /// <response code="400">Si el BotId proporcionado no existe.</response>
        [HttpPost]
        [HasPermission("CanCreateBotActions")]
        public async Task<ActionResult<BotAction>> Create([FromBody] CreateBotActionDto dto)
        {
            // Verificar si el BotId existe en la base de datos
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
            {
                return BadRequest(new { message = "BotId does not exist in the system." });
            }

            var action = new BotAction
            {
                BotId = dto.BotId,
                TriggerPhrase = dto.TriggerPhrase,
                ActionType = dto.ActionType,
                Payload = dto.Payload,
                CreatedAt = DateTime.UtcNow
            };

            _context.BotActions.Add(action);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = action.Id }, action);
        }

        /// <summary>
        /// Actualiza una acción de bot existente.
        /// </summary>
        /// <param name="id">ID de la acción de bot que se desea actualizar.</param>
        /// <param name="dto">Datos actualizados de la acción de bot.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">Acción actualizada correctamente.</response>
        /// <response code="404">Si la acción de bot no existe.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateBotActions")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBotActionDto dto)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound(new { message = "Bot action not found." });

            // Actualizar los campos de la acción si no son nulos
            if (dto.TriggerPhrase != null) action.TriggerPhrase = dto.TriggerPhrase;
            if (dto.ActionType != null) action.ActionType = dto.ActionType;
            if (dto.Payload != null) action.Payload = dto.Payload;

            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Elimina una acción de bot.
        /// </summary>
        /// <param name="id">ID de la acción de bot a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">Acción eliminada correctamente.</response>
        /// <response code="404">Si la acción de bot no existe.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteBotActions")]
        public async Task<IActionResult> Delete(int id)
        {
            var action = await _context.BotActions.FindAsync(id);
            if (action == null)
                return NotFound(new { message = "Bot action not found." });

            _context.BotActions.Remove(action);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
