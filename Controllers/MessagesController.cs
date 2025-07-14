using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Messages;
using Voia.Api.Models.Messages.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Voia.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
        {
            var messages = await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Bot)
                .Include(m => m.Conversation)
                .ToListAsync();
            return Ok(messages);
        }

        [HttpGet("by-conversation/{conversationId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessagesByConversation(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }


        [HttpPost]
        public async Task<ActionResult<Message>> CreateMessage([FromBody] CreateMessageDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound(new { message = $"User with ID {dto.UserId} not found." });

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
                return NotFound(new { message = $"Bot with ID {dto.BotId} not found." });

            var message = new Message
            {
                BotId = dto.BotId,
                UserId = dto.UserId,
                ConversationId = dto.ConversationId,
                Sender = dto.Sender,
                MessageText = dto.MessageText,
                TokensUsed = dto.TokensUsed ?? 0,
                Source = dto.Source ?? "widget",
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = dto.ReplyToMessageId // ✅ importante
            };


            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessages), new { id = message.Id }, message);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(int id, [FromBody] UpdateMessageDto dto)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound(new { message = "Message not found." });

            message.BotId = dto.BotId;
            message.UserId = dto.UserId;
            message.ConversationId = dto.ConversationId;
            message.Sender = dto.Sender;
            message.MessageText = dto.MessageText;
            message.TokensUsed = dto.TokensUsed ?? 0;
            message.Source = dto.Source;

            await _context.SaveChangesAsync();
            return Ok(message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound(new { message = "Message not found." });

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
