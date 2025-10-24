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

                [HttpGet("by-bot/{botId}")]
                [HasPermission("CanViewBotCustomPrompts")]
                public async Task<ActionResult<IEnumerable<BotCustomPromptResponseDto>>> GetByBotId(int botId)
                {
                    var prompts = await _context.BotCustomPrompts
                        .Where(p => p.BotId == botId)
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

        [HttpGet]
    [HasPermission("CanViewBotCustomPrompts")]
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
    [HasPermission("CanViewBotCustomPrompts")]
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
    [HasPermission("CanEditBotCustomPrompts")]
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

            // Marcar fase 'training' como completada para el bot (non-blocking)
            try
            {
                var meta = System.Text.Json.JsonSerializer.Serialize(new { source = "prompt_create", promptId = prompt.Id, role = prompt.Role.ToString() });
                var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == prompt.BotId && p.Phase == "training");
                if (phase == null)
                {
                    _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = prompt.BotId, Phase = "training", CompletedAt = DateTime.UtcNow, Meta = meta, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
                else
                {
                    phase.CompletedAt = DateTime.UtcNow;
                    phase.Meta = meta;
                    phase.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
            catch { /* non-blocking */ }

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
    [HasPermission("CanEditBotCustomPrompts")]
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
    [HasPermission("CanDeleteBotCustomPrompts")]
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
