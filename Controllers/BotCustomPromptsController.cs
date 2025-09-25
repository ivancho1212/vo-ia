using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Data;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotCustomPromptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotCustomPromptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotCustomPromptResponseDto>>> GetAll()
        {
            var prompts = await _context.BotCustomPrompts
                .Select(p => new BotCustomPromptResponseDto
                {
                    Id = p.Id,
                    BotId = p.BotId,
                    Role = p.Role,
                    Content = p.Content,
                    BotTemplateId = p.BotTemplateId,
                    TemplateTrainingSessionId = p.TemplateTrainingSessionId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(prompts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BotCustomPromptResponseDto>> GetById(int id)
        {
            var p = await _context.BotCustomPrompts.FindAsync(id);

            if (p == null)
                return NotFound();

            var dto = new BotCustomPromptResponseDto
            {
                Id = p.Id,
                BotId = p.BotId,
                Role = p.Role,
                Content = p.Content,
                BotTemplateId = p.BotTemplateId,
                TemplateTrainingSessionId = p.TemplateTrainingSessionId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<BotCustomPromptResponseDto>> Create(BotCustomPromptCreateDto dto)
        {
            // Validación de existencia del bot
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
                return BadRequest($"El Bot con ID {dto.BotId} no existe.");

            // Validación de existencia del template si se proporciona
            if (dto.BotTemplateId.HasValue)
            {
                var templateExists = await _context.BotTemplates.AnyAsync(t => t.Id == dto.BotTemplateId);
                if (!templateExists)
                    return BadRequest($"El BotTemplate con ID {dto.BotTemplateId} no existe.");
            }

            var prompt = new BotCustomPrompt
            {
                BotId = dto.BotId,
                Role = dto.Role,
                Content = dto.Content,
                BotTemplateId = dto.BotTemplateId,
                TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotCustomPrompts.Add(prompt);
            await _context.SaveChangesAsync();

            var response = new BotCustomPromptResponseDto
            {
                Id = prompt.Id,
                BotId = prompt.BotId,
                Role = prompt.Role,
                Content = prompt.Content,
                BotTemplateId = prompt.BotTemplateId,
                TemplateTrainingSessionId = prompt.TemplateTrainingSessionId,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = prompt.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotCustomPromptUpdateDto dto)
        {
            var prompt = await _context.BotCustomPrompts.FindAsync(id);
            if (prompt == null)
                return NotFound();

            if (dto.BotTemplateId.HasValue)
            {
                var templateExists = await _context.BotTemplates.AnyAsync(t => t.Id == dto.BotTemplateId);
                if (!templateExists)
                    return BadRequest($"El BotTemplate con ID {dto.BotTemplateId} no existe.");
            }

            prompt.Role = dto.Role;
            prompt.Content = dto.Content;
            prompt.BotTemplateId = dto.BotTemplateId;
            prompt.TemplateTrainingSessionId = dto.TemplateTrainingSessionId;
            prompt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var prompt = await _context.BotCustomPrompts.FindAsync(id);
            if (prompt == null)
                return NotFound();

            _context.BotCustomPrompts.Remove(prompt);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
