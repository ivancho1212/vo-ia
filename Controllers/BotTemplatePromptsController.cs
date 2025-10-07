using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotTemplatePromptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotTemplatePromptsController(ApplicationDbContext context)
        {
            _context = context;
        }

    [HttpGet]
    [HasPermission("CanViewBotTemplatePrompts")]
    public async Task<ActionResult<IEnumerable<BotTemplatePromptResponseDto>>> GetAll()
        {
            var prompts = await _context.BotTemplatePrompts
                .Select(p => new BotTemplatePromptResponseDto
                {
                    Id = p.Id,
                    BotTemplateId = p.BotTemplateId,
                    Role = p.Role.ToString(),
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(prompts);
        }

    [HttpGet("{id}")]
    [HasPermission("CanViewBotTemplatePrompts")]
    public async Task<ActionResult<BotTemplatePromptResponseDto>> GetById(int id)
        {
            var p = await _context.BotTemplatePrompts.FindAsync(id);

            if (p == null) return NotFound();

            return new BotTemplatePromptResponseDto
            {
                Id = p.Id,
                BotTemplateId = p.BotTemplateId,
                Role = p.Role.ToString(),
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

    [HttpPost]
    [HasPermission("CanEditBotTemplatePrompts")]
    public async Task<ActionResult<BotTemplatePromptResponseDto>> Create(BotTemplatePromptCreateDto dto)
        {
            if (!Enum.TryParse<PromptRole>(dto.Role, out var role))
                return BadRequest("Invalid role. Must be 'system', 'user', or 'assistant'.");

            var prompt = new BotTemplatePrompt
            {
                BotTemplateId = dto.BotTemplateId,
                Role = role,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotTemplatePrompts.Add(prompt);
            await _context.SaveChangesAsync();

            var response = new BotTemplatePromptResponseDto
            {
                Id = prompt.Id,
                BotTemplateId = prompt.BotTemplateId,
                Role = prompt.Role.ToString(),
                Content = prompt.Content,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = prompt.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotTemplatePromptCreateDto dto)
        {
            var prompt = await _context.BotTemplatePrompts.FindAsync(id);
            if (prompt == null) return NotFound();

            if (!Enum.TryParse<PromptRole>(dto.Role, out var role))
                return BadRequest("Invalid role.");

            prompt.BotTemplateId = dto.BotTemplateId;
            prompt.Role = role;
            prompt.Content = dto.Content;
            prompt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var prompt = await _context.BotTemplatePrompts.FindAsync(id);
            if (prompt == null) return NotFound();

            _context.BotTemplatePrompts.Remove(prompt);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
