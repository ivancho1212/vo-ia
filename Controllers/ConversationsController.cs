using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Conversations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConversationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/conversations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations()
        {
            var conversations = await _context.Conversations
                .Include(c => c.User)  // Incluye los datos del usuario
                .Include(c => c.Bot)   // Incluye los datos del bot
                .ToListAsync();
            return Ok(conversations);
        }

        // POST: api/conversations
        [HttpPost]
        public async Task<ActionResult<Conversation>> CreateConversation(CreateConversationDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {dto.UserId} not found.");
            }

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
            {
                return NotFound($"Bot with ID {dto.BotId} not found.");
            }

            var conversation = new Conversation
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                Title = dto.Title,
                UserMessage = dto.UserMessage,
                BotResponse = dto.BotResponse,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConversations), new { id = conversation.Id }, conversation);
        }

        // PUT: api/conversations/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConversation(int id, UpdateConversationDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound($"Conversation with ID {id} not found.");
            }

            // Actualiza solo los campos permitidos
            conversation.Title = dto.Title ?? conversation.Title;
            conversation.UserMessage = dto.UserMessage ?? conversation.UserMessage;
            conversation.BotResponse = dto.BotResponse ?? conversation.BotResponse;

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return Ok(conversation);
        }
        // DELETE: api/conversations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
            {
                return NotFound(new { message = $"Conversation with ID {id} not found." });
            }

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Conversation with ID {id} was deleted successfully." });
        }

    }
}
