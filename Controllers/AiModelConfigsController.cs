using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.AiModelConfigs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiModelConfigsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AiModelConfigsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ai_model_configs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AiModelConfig>>> GetAll()
        {
            return await _context.AiModelConfigs.ToListAsync();
        }

        // GET: api/ai_model_configs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AiModelConfig>> GetById(int id)
        {
            var config = await _context.AiModelConfigs.FindAsync(id);
            if (config == null)
                return NotFound(new { message = "AI model config not found." });

            return Ok(config);
        }

        // POST: api/ai_model_configs
        [HttpPost]
        public async Task<ActionResult<AiModelConfig>> Create([FromBody] AiModelConfigCreateDto dto)
        {
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


        // PUT: api/ai_model_configs/2
        [HttpPut("{id}")]
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

            return NoContent(); // O puedes retornar Ok(existing) si prefieres
        }


        // DELETE: api/ai_model_configs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _context.AiModelConfigs.FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Config not found." });

            _context.AiModelConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
