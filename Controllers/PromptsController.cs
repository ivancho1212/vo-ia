using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Prompts;
using Voia.Api.Models.Prompts.DTOs;

using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PromptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/prompts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Prompt>>> GetPrompts()
        {
            var prompts = await _context.Prompts
                .Include(p => p.User)
                .Include(p => p.Bot)
                .Include(p => p.Conversation)
                .ToListAsync();
            return Ok(prompts);
        }

        // POST: api/prompts
        [HttpPost]
        public async Task<ActionResult<Prompt>> CreatePrompt(CreatePromptDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"User with ID {dto.UserId} not found.");

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null) return NotFound($"Bot with ID {dto.BotId} not found.");

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
        [HttpPut("{id}")]
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrompt(int id)
        {
            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
            {
                return NotFound(new { message = "Prompt not found." });
            }

            _context.Prompts.Remove(prompt);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 sin cuerpo
        }

    }
}
