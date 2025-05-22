// Controllers/BotCustomPromptsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
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
                    TrainingSessionId = p.TrainingSessionId,
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

            return new BotCustomPromptResponseDto
            {
                Id = p.Id,
                BotId = p.BotId,
                Role = p.Role,
                Content = p.Content,
                TrainingSessionId = p.TrainingSessionId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

        [HttpPost]
        public async Task<ActionResult<BotCustomPromptResponseDto>> Create(BotCustomPromptCreateDto dto)
        {
            var prompt = new BotCustomPrompt
            {
                BotId = dto.BotId,
                Role = dto.Role,
                Content = dto.Content,
                TrainingSessionId = dto.TrainingSessionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotCustomPrompts.Add(prompt);
            await _context.SaveChangesAsync();

            var responseDto = new BotCustomPromptResponseDto
            {
                Id = prompt.Id,
                BotId = prompt.BotId,
                Role = prompt.Role,
                Content = prompt.Content,
                TrainingSessionId = prompt.TrainingSessionId,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = prompt.Id }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotCustomPromptUpdateDto dto)
        {
            var prompt = await _context.BotCustomPrompts.FindAsync(id);

            if (prompt == null)
                return NotFound();

            prompt.Role = dto.Role;
            prompt.Content = dto.Content;
            prompt.TrainingSessionId = dto.TrainingSessionId;
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
