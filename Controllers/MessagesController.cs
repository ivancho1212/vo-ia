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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Voia.Api.Hubs;
using Microsoft.Extensions.Logging;

using Voia.Api.Services.Security;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Super Admin")]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessagesController> _logger;
        private readonly ISanitizationService _sanitizer;

        public MessagesController(ApplicationDbContext context, IHubContext<ChatHub> hubContext, ILogger<MessagesController> logger, ISanitizationService sanitizer)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _sanitizer = sanitizer;
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

        // Obtener mensajes por conversación (incluye datos de archivos adjuntos)
        [HttpGet("by-conversation/{conversationId}")]
        public async Task<ActionResult> GetMessagesByConversation(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.ChatUploadedFile)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var result = messages.Select(m => new
            {
                id = m.Id,
                conversationId = m.ConversationId,
                botId = m.BotId,
                userId = m.UserId,
                publicUserId = m.PublicUserId,
                sender = m.Sender,
                messageText = m.MessageText,
                tokensUsed = m.TokensUsed,
                source = m.Source,
                createdAt = m.CreatedAt,
                read = m.Read,
                replyToMessageId = m.ReplyToMessageId,
                tempId = m.TempId,
                fileId = m.FileId,
                status = m.Status,
                // ✅ Datos del archivo adjunto con URL de API segura
                fileName = m.ChatUploadedFile?.FileName,
                fileType = m.ChatUploadedFile?.FileType,
                fileUrl = m.ChatUploadedFile != null ? $"/api/files/chat/{m.ChatUploadedFile.Id}" : null
            });

            return Ok(result);
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

    // Marcar mensajes como leídos (permitir a admins marcar como leídos desde la UI)
    [HttpPost("mark-read/{conversationId}")]
    [Authorize(Roles = "Admin")]
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
            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
                return NotFound(new { message = $"Bot with ID {dto.BotId} not found." });
            // If caller didn't supply UserId/PublicUserId, try to resolve from the conversation
            var conversation = await _context.Conversations.FindAsync(dto.ConversationId);
            if (conversation == null)
                return NotFound(new { message = $"Conversation with ID {dto.ConversationId} not found." });

            // Resolve identity: prefer explicit values from DTO; fallback to conversation.PublicUserId
            int? resolvedUserId = dto.UserId;
            int? resolvedPublicUserId = dto.PublicUserId ?? conversation.PublicUserId;

            if (!resolvedUserId.HasValue && !resolvedPublicUserId.HasValue)
            {
                return BadRequest(new { message = "Se requiere UserId o PublicUserId (no se pudo resolver desde la conversación)." });
            }

            if (resolvedUserId.HasValue)
            {
                var user = await _context.Users.FindAsync(resolvedUserId.Value);
                if (user == null)
                    return NotFound(new { message = $"User with ID {resolvedUserId} not found." });
            }

            if (resolvedPublicUserId.HasValue)
            {
                var publicUser = await _context.PublicUsers.FindAsync(resolvedPublicUserId.Value);
                if (publicUser == null)
                    return NotFound(new { message = $"PublicUser with ID {resolvedPublicUserId} not found." });
            }
            var message = new Message
            {
                BotId = dto.BotId,
                UserId = resolvedUserId,
                PublicUserId = resolvedPublicUserId,
                ConversationId = dto.ConversationId,
                Sender = dto.Sender,
                MessageText = _sanitizer.SanitizeText(dto.MessageText),
                TokensUsed = dto.TokensUsed ?? 0,
                Source = dto.Source ?? "widget",
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = dto.ReplyToMessageId,
                Read = dto.Sender != "user"
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Notify connected admins about the new message/conversation
            try
            {
                var msgDto = new
                {
                    id = message.Id,
                    conversationId = message.ConversationId,
                    text = message.MessageText,
                    from = message.Sender,
                    fromRole = message.Sender == "admin" ? "admin" : "user",
                    timestamp = message.CreatedAt,
                    userId = message.UserId,
                    publicUserId = message.PublicUserId
                };

                await _hubContext.Clients.Group("admin").SendAsync("NewConversationOrMessage", msgDto);
                _logger?.LogInformation("Sent NewConversationOrMessage for message {MessageId} conv {ConversationId}", message.Id, message.ConversationId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to notify admins about new message {MessageId}", message.Id);
            }

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
            message.MessageText = _sanitizer.SanitizeText(dto.MessageText);
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