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
            // Verificar si el BotId y el UserId son válidos
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
                Title = dto.Title,  // Asegúrate de que el título se pase desde el DTO
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConversations), new { id = conversation.Id }, conversation);
        }
    }
}
