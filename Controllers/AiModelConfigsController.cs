using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.AiModelConfigs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class AiModelConfigsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AiModelConfigsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las configuraciones del modelo AI.
        /// </summary>
        /// <returns>Lista de configuraciones del modelo AI.</returns>
        /// <response code="200">Devuelve la lista de configuraciones del modelo AI.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewAiModelConfigs")]
        public async Task<ActionResult<IEnumerable<AiModelConfig>>> GetAll()
        {
            return await _context.AiModelConfigs.ToListAsync();
        }

        /// <summary>
        /// Obtiene la configuración de un modelo AI por su ID.
        /// </summary>
        /// <param name="id">ID de la configuración del modelo AI.</param>
        /// <returns>La configuración del modelo AI.</returns>
        /// <response code="200">Devuelve la configuración del modelo AI.</response>
        /// <response code="404">Si no se encuentra la configuración del modelo AI.</response>
        [HttpGet("{id}")]
        [HasPermission("CanViewAiModelConfigs")]
        public async Task<ActionResult<AiModelConfig>> GetById(int id)
        {
            var config = await _context.AiModelConfigs.FindAsync(id);
            if (config == null)
                return NotFound(new { message = "AI model config not found." });

            return Ok(config);
        }

        /// <summary>
        /// Crea una nueva configuración del modelo AI.
        /// </summary>
        /// <param name="dto">Datos para crear la configuración del modelo AI.</param>
        /// <returns>La configuración del modelo AI creada.</returns>
        /// <response code="201">Devuelve la configuración del modelo AI creada.</response>
        /// <response code="400">Si el BotId proporcionado no existe.</response>
        [HttpPost]
        [HasPermission("CanCreateAiModelConfigs")]
        public async Task<ActionResult<AiModelConfig>> Create([FromBody] AiModelConfigCreateDto dto)
        {
            // Verificar si el BotId existe en la base de datos
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
            {
                return BadRequest(new { message = "BotId does not exist in the system." });
            }

            var config = new AiModelConfig
            {
                BotId = dto.BotId,
                ModelName = dto.ModelName,
                Temperature = dto.Temperature,
                MaxTokens = dto.MaxTokens,
                FrequencyPenalty = dto.FrequencyPenalty,
                PresencePenalty = dto.PresencePenalty,
                CreatedAt = DateTime.UtcNow
            };

            _context.AiModelConfigs.Add(config);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = config.Id }, config);
        }

        /// <summary>
        /// Actualiza una configuración del modelo AI existente.
        /// </summary>
        /// <param name="id">ID de la configuración del modelo AI que se desea actualizar.</param>
        /// <param name="dto">Datos actualizados de la configuración del modelo AI.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="204">Configuración actualizada correctamente.</response>
        /// <response code="404">Si la configuración del modelo AI no existe.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateAiModelConfigs")]
        public async Task<IActionResult> Update(int id, [FromBody] AiModelConfigUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.AiModelConfigs.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Config not found." });

            // Actualizar campos
            existing.BotId = dto.BotId;
            existing.ModelName = dto.ModelName;
            existing.Temperature = dto.Temperature;
            existing.MaxTokens = dto.MaxTokens;
            existing.FrequencyPenalty = dto.FrequencyPenalty;
            existing.PresencePenalty = dto.PresencePenalty;

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Elimina una configuración del modelo AI.
        /// </summary>
        /// <param name="id">ID de la configuración del modelo AI a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">Configuración eliminada correctamente.</response>
        /// <response code="404">Si la configuración del modelo AI no existe.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteAiModelConfigs")]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _context.AiModelConfigs.FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Config not found." });

            _context.AiModelConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
