using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Messages;
using Voia.Api.Models.Messages.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Voia.Api.Attributes;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [BotTokenAuthorize]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtener todos los mensajes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
        {
            var messages = await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Bot)
                .Include(m => m.PublicUser)
                .ToListAsync();

            return Ok(messages);
        }

        // Obtener mensajes por conversación
        [HttpGet("by-conversation/{conversationId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessagesByConversation(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

        // Obtener solo mensajes no leídos de una conversación
        [HttpGet("unread/by-conversation/{conversationId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetUnreadMessagesByConversation(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.Read == false && m.Sender == "user")
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

        // Marcar mensajes como leídos
        [HttpPost("mark-read/{conversationId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.Read == false && m.Sender == "user")
                .ToListAsync();

            if (!messages.Any())
                return Ok(new { message = "No unread messages to update." });

            foreach (var message in messages)
            {
                message.Read = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{messages.Count} message(s) marked as read." });
        }

        [HttpPost]
        public async Task<ActionResult<Message>> CreateMessage([FromBody] CreateMessageDto dto)
        {
            // Validaciones
            if (dto.UserId == null && dto.PublicUserId == null)
                return BadRequest(new { message = "Se requiere UserId o PublicUserId." });

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
                return NotFound(new { message = $"Bot with ID {dto.BotId} not found." });

            if (dto.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(dto.UserId.Value);
                if (user == null)
                    return NotFound(new { message = $"User with ID {dto.UserId} not found." });
            }

            if (dto.PublicUserId.HasValue)
            {
                var publicUser = await _context.PublicUsers.FindAsync(dto.PublicUserId.Value);
                if (publicUser == null)
                    return NotFound(new { message = $"PublicUser with ID {dto.PublicUserId} not found." });
            }

            var message = new Message
            {
                BotId = dto.BotId,
                UserId = dto.UserId,
                PublicUserId = dto.PublicUserId,
                ConversationId = dto.ConversationId,
                Sender = dto.Sender,
                MessageText = dto.MessageText,
                TokensUsed = dto.TokensUsed ?? 0,
                Source = dto.Source ?? "widget",
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = dto.ReplyToMessageId,
                Read = dto.Sender != "user"
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessages), new { id = message.Id }, message);
        }

        // Actualizar mensaje
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(int id, [FromBody] UpdateMessageDto dto)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound(new { message = "Message not found." });

            message.BotId = dto.BotId;
            message.UserId = dto.UserId;
            message.PublicUserId = dto.PublicUserId;
            message.ConversationId = dto.ConversationId;
            message.Sender = dto.Sender;
            message.MessageText = dto.MessageText;
            message.TokensUsed = dto.TokensUsed ?? 0;
            message.Source = dto.Source;
            message.ReplyToMessageId = dto.ReplyToMessageId;

            await _context.SaveChangesAsync();
            return Ok(message);
        }

        // Eliminar mensaje
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