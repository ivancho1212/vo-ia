using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Controllers
{
    //[Authorize(Roles = "Admin,User")]
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
        public async Task<ActionResult<IEnumerable<AiModelConfigDto>>> GetAll()
        {
            var configs = await _context
                .AiModelConfigs.Include(c => c.IaProvider) // <- esto es clave
                .ToListAsync();

            var result = configs.Select(c => new AiModelConfigDto
            {
                Id = c.Id,
                ModelName = c.ModelName,
                Temperature = c.Temperature,
                FrequencyPenalty = c.FrequencyPenalty,
                PresencePenalty = c.PresencePenalty,
                CreatedAt = c.CreatedAt,
                IaProviderName = c.IaProvider?.Name,
                IaProviderId = c.IaProviderId,
            });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene la configuración de un modelo AI por su ID.
        /// </summary>
        /// <param name="id">ID de la configuración del modelo AI.</param>
        /// <returns>La configuración del modelo AI.</returns>
        /// <response code="200">Devuelve la configuración del modelo AI.</response>
        /// <response code="404">Si no se encuentra la configuración del modelo AI.</response>
        [HttpGet("{id}")]
        //[HasPermission("CanViewAiModelConfigs")]
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
        public async Task<ActionResult<AiModelConfig>> Create([FromBody] AiModelConfigCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // Validar que el proveedor existe (opcional, pero recomendado)
            var providerExists = await _context.BotIaProviders.AnyAsync(p =>
                p.Id == dto.IaProviderId
            );
            if (!providerExists)
                return BadRequest(new { message = "IA Provider not found." });

            var config = new AiModelConfig
            {
                ModelName = dto.ModelName,
                Temperature = dto.Temperature,
                FrequencyPenalty = dto.FrequencyPenalty,
                PresencePenalty = dto.PresencePenalty,
                IaProviderId = dto.IaProviderId, // asigna el campo!
                CreatedAt = DateTime.UtcNow,
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
        //[HasPermission("CanUpdateAiModelConfigs")]
        public async Task<IActionResult> Update(int id, [FromBody] AiModelConfigUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.AiModelConfigs.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Config not found." });

            // Actualizar campos
            existing.ModelName = dto.ModelName;
            existing.Temperature = dto.Temperature;
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
        //[HasPermission("CanDeleteAiModelConfigs")]
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
