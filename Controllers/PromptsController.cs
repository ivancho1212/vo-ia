using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Prompts;
using Voia.Api.Models.Prompts.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class PromptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PromptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los prompts.
        /// </summary>
        /// <returns>Lista de prompts asociados a usuarios y bots.</returns>
        /// <response code="200">Devuelve la lista de prompts.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewPrompts")]
        public async Task<ActionResult<IEnumerable<Prompt>>> GetPrompts()
        {
            var prompts = await _context.Prompts
                .Include(p => p.User)
                .Include(p => p.Bot)
                .Include(p => p.Conversation)
                .ToListAsync();
            return Ok(prompts);
        }

        /// <summary>
        /// Crea un nuevo prompt.
        /// </summary>
        /// <param name="dto">Datos del nuevo prompt.</param>
        /// <returns>El prompt recién creado.</returns>
        /// <response code="201">El prompt fue creado exitosamente.</response>
        /// <response code="400">Si los datos del prompt son inválidos.</response>
        /// <response code="404">Si no se encuentran el usuario o el bot.</response>
        [HttpPost]
        [HasPermission("CanCreatePrompts")]
        public async Task<ActionResult<Prompt>> CreatePrompt([FromBody] CreatePromptDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound(new { message = $"User with ID {dto.UserId} not found." });

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
                return NotFound(new { message = $"Bot with ID {dto.BotId} not found." });

            Prompt prompt = new Prompt
            {
                BotId = dto.BotId,
                UserId = dto.UserId,
                ConversationId = dto.ConversationId,
                PromptText = dto.PromptText,
                ResponseText = dto.ResponseText,
                TokensUsed = dto.TokensUsed ?? 0,
                CreatedAt = DateTime.UtcNow,
                Source = dto.Source ?? "widget"
            };

            _context.Prompts.Add(prompt);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPrompts), new { id = prompt.Id }, prompt);
        }

        /// <summary>
        /// Actualiza un prompt existente.
        /// </summary>
        /// <param name="id">ID del prompt a actualizar.</param>
        /// <param name="dto">Datos actualizados del prompt.</param>
        /// <returns>El prompt actualizado.</returns>
        /// <response code="200">El prompt fue actualizado correctamente.</response>
        /// <response code="400">Si los datos son inválidos.</response>
        /// <response code="404">Si no se encuentra el prompt.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdatePrompts")]
        public async Task<IActionResult> UpdatePrompt(int id, [FromBody] UpdatePromptDto dto)
        {
            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
            {
                return NotFound(new { message = "Prompt not found." });
            }

            // Actualizar campos
            prompt.BotId = dto.BotId;
            prompt.UserId = dto.UserId;
            prompt.ConversationId = dto.ConversationId;
            prompt.PromptText = dto.PromptText;
            prompt.ResponseText = dto.ResponseText;
            prompt.TokensUsed = dto.TokensUsed ?? 0;
            prompt.Source = dto.Source;

            await _context.SaveChangesAsync();

            return Ok(prompt);
        }

        /// <summary>
        /// Elimina un prompt por su ID.
        /// </summary>
        /// <param name="id">ID del prompt a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">El prompt fue eliminado correctamente.</response>
        /// <response code="404">Si no se encuentra el prompt.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeletePrompts")]
        public async Task<IActionResult> DeletePrompt(int id)
        {
            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
            {
                return NotFound(new { message = "Prompt not found." });
            }

            _context.Prompts.Remove(prompt);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
