using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Conversations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Voia.Api.Hubs;
using Voia.Api.Models.Messages;
using System.Linq; // Necesario para .Concat y .OrderBy

namespace Voia.Api.Controllers
{
    // Modelo DTO para la actualización de estado
    public class UpdateStatusDto
    {
        public string Status { get; set; }
    }

    // [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ConversationsController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Obtiene todas las conversaciones con los datos relacionados de usuario y bot.
        /// </summary>
        [HttpGet]
        // [HasPermission("CanViewConversations")]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations()
        {
            var conversations = await _context.Conversations
                .Include(c => c.User)
                .Include(c => c.Bot)
                .Select(c => new
                {
                    c.Id,
                    c.Status,
                    Title = c.Title ?? string.Empty,
                    UserMessage = c.UserMessage ?? string.Empty,
                    BotResponse = c.BotResponse ?? string.Empty,
                    User = c.User != null ? new { c.User.Name, c.User.Email } : null,
                    Bot = c.Bot != null ? new { c.Bot.Name } : null
                })
                .ToListAsync();

            return Ok(conversations);
        }
        
        [HttpPost("{id}/disconnect")]
        public async Task<IActionResult> UserDisconnected(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation != null)
            {
                conversation.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        /// <summary>
        /// Devuelve las conversaciones asociadas a los bots de un usuario específico.
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetConversationsByUser(int userId, int page = 1, int limit = 10)
        {
            try
            {
                var query = _context.Conversations
                    .Include(c => c.User)
                    .Include(c => c.Bot)
                    .Where(c => c.Bot.UserId == userId);

                var total = await query.CountAsync();

                var conversations = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.Id,
                        c.Status,
                        Title = c.Title ?? string.Empty,
                        UserMessage = c.UserMessage ?? string.Empty,
                        BotResponse = c.BotResponse ?? string.Empty,
                        CreatedAt = c.CreatedAt,
                        User = c.User != null ? new { c.User.Name, c.User.Email } : null,
                        Bot = c.Bot != null ? new { c.Bot.Name } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    page,
                    limit,
                    total,
                    conversations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener conversaciones.", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el historial completo de una conversación, incluyendo mensajes y archivos.
        /// </summary>
        [HttpGet("history/{conversationId}")]
        public async Task<IActionResult> GetConversationHistory(int conversationId)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return NotFound("Conversación no encontrada");

            // --- Mapeo de Mensajes ---
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.User)
                .Include(m => m.Bot)
                .Select(m => new ConversationItemDto
                {
                    Id = m.Id,
                    Type = "message",
                    Text = m.MessageText,
                    Timestamp = m.CreatedAt,
                    FromRole = m.Sender,
                    FromId = m.Sender == "user" ? m.UserId : m.BotId,
                    FromName = m.Sender == "user" ? (m.User != null ? m.User.Name : "Usuario") : (m.Bot != null ? m.Bot.Name : "Bot"),
                    ReplyToMessageId = m.ReplyToMessageId
                })
                .ToListAsync();

            // --- Mapeo de Archivos ---
            var files = await _context.ChatUploadedFiles
                .AsNoTracking()
                .Where(f => f.ConversationId == conversationId)
                .Include(f => f.User)
                .Select(f => new ConversationItemDto
                {
                    Id = f.Id,
                    Type = "file",
                    Timestamp = f.UploadedAt ?? DateTime.UtcNow,
                    FromRole = "user",
                    FromId = f.UserId,
                    FromName = f.User != null ? f.User.Name : "Usuario",
                    FileUrl = f.FilePath,
                    FileName = f.FileName,
                    FileType = f.FileType
                })
                .ToListAsync();

            // --- Combinar y Ordenar ---
            var combinedHistory = messages.Concat(files)
                .OrderBy(item => item.Timestamp)
                .ToList();

            return Ok(new
            {
                conversationDetails = new
                {
                    id = conversation.Id,
                    title = conversation.Title,
                    status = conversation.Status
                },
                history = combinedHistory
            });
        }

        /// <summary>
        /// Actualiza el estado de una conversación específica.
        /// </summary>
        [HttpPatch("{id}/status")]
        // [HasPermission("CanUpdateConversationStatus")]
        public async Task<IActionResult> UpdateConversationStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(new { message = "El nuevo estado no puede ser nulo o vacío." });
            }

            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound(new { message = $"Conversación con ID {id} no encontrada." });
            }

            conversation.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Estado de la conversación {id} actualizado a '{dto.Status}'." });
        }

        /// <summary>
        /// Actualiza una conversación existente.
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("CanUpdateConversations")] // Esta anotación no existe, se comenta
        public async Task<IActionResult> UpdateConversation(int id, [FromBody] Conversation dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound(new { message = $"Conversation with ID {id} not found." });
            }

            conversation.Title = dto.Title ?? conversation.Title;
            conversation.UserMessage = dto.UserMessage ?? conversation.UserMessage;
            conversation.BotResponse = dto.BotResponse ?? conversation.BotResponse;

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return Ok(conversation);
        }

        /// <summary>
        /// Elimina una conversación por su ID.
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("CanDeleteConversations")] // Esta anotación no existe, se comenta
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