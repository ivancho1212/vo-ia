using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Conversations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR; // ✅ Necesario para IHubContext
using Voia.Api.Hubs; // ✅ Asegúrate de que este sea el namespace donde está tu ChatHub
using Voia.Api.Models.Messages;

namespace Voia.Api.Controllers
{
    //[Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext; // ✅ Nuevo campo para SignalR

        // 👇 Modificamos el constructor para incluir IHubContext
        public ConversationsController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Obtiene todas las conversaciones con los datos relacionados de usuario y bot.
        /// </summary>
        /// <returns>Lista de conversaciones.</returns>
        /// <response code="200">Devuelve una lista de todas las conversaciones.</response>
        /// <response code="500">Si ocurre un error interno.</response>
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
                    Title = c.Title ?? string.Empty,  // Manejo de NULL en Title
                    UserMessage = c.UserMessage ?? string.Empty,  // Manejo de NULL en UserMessage
                    BotResponse = c.BotResponse ?? string.Empty,  // Manejo de NULL en BotResponse
                    User = c.User != null ? new { c.User.Name, c.User.Email } : null,
                    Bot = c.Bot != null ? new { c.Bot.Name } : null
                })
                .ToListAsync();

            return Ok(conversations);
        }

        /// <summary>
        /// Devuelve las conversaciones asociadas a los bots de un usuario específico.
        /// Temporalmente sin autorización mientras se configura el flujo.
        /// </summary>
        [HttpGet("by-user/{userId}")]
        //[AllowAnonymous] // Temporal para pruebas, luego reemplazar por [Authorize]
        public async Task<IActionResult> GetConversationsByUser(int userId)
        {
            try
            {
                var conversations = await _context.Conversations
                    .Include(c => c.User)
                    .Include(c => c.Bot)
                    .Where(c => c.Bot.UserId == userId) // 👈 Filtra por bots del usuario
                    .Select(c => new
                    {
                        c.Id,
                        Title = c.Title ?? string.Empty,
                        UserMessage = c.UserMessage ?? string.Empty,
                        BotResponse = c.BotResponse ?? string.Empty,
                        CreatedAt = c.CreatedAt,
                        User = c.User != null ? new { c.User.Name, c.User.Email } : null,
                        Bot = c.Bot != null ? new { c.Bot.Name } : null
                    })
                    .ToListAsync();

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener conversaciones.", error = ex.Message });
            }
        }
        [HttpGet("history/{conversationId}")]
        public async Task<IActionResult> GetConversationHistory(int conversationId)
        {
            var conversation = await _context.Conversations
                .Include(c => c.User)
                .Include(c => c.Bot)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return NotFound("Conversación no encontrada");

            var userId = conversation.UserId;
            var botId = conversation.BotId;

            // ✅ Traer mensajes con info del usuario y bot
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.User)
                .Include(m => m.Bot)
                .Select(m => new ConversationItemDto
                {
                    Type = "message",
                    Text = m.MessageText,
                    Timestamp = m.CreatedAt,
                    FromRole = m.Sender, // "user", "bot", "admin"
                    FromId = m.Sender == "user" ? m.UserId : m.Sender == "bot" ? m.BotId : null,
                    FromName = m.Sender == "user" ? m.User.Name : m.Sender == "bot" ? m.Bot.Name : "admin",
                })
                .ToListAsync();

            // ✅ Traer archivos con info del usuario que los subió
            var files = await _context.ChatUploadedFiles
                .Where(f => f.ConversationId == conversationId)
                .Include(f => f.User)
                .Select(f => new ConversationItemDto
                {
                    Type = "file",
                    FileName = f.FileName,
                    FileType = f.FileType,
                    FileUrl = f.FilePath,
                    Timestamp = f.UploadedAt ?? DateTime.UtcNow,
                    FromRole = "user",
                    FromId = f.UserId,
                    FromName = f.User.Name,
                })
                .ToListAsync();

            var combined = messages
                .Concat(files)
                .OrderBy(item => item.Timestamp)
                .ToList();

            return Ok(combined);
        }
        /// <summary>
        /// Actualiza una conversación existente.
        /// </summary>
        /// <param name="id">ID de la conversación que se desea actualizar.</param>
        /// <param name="dto">Datos actualizados de la conversación.</param>
        /// <returns>Resultado de la actualización.</returns>
        /// <response code="200">Configuración actualizada correctamente.</response>
        /// <response code="404">Si la conversación no existe.</response>
        [HttpPut("{id}")]
        [HasPermission("CanUpdateConversations")]
        public async Task<IActionResult> UpdateConversation(int id, [FromBody] UpdateConversationDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound(new { message = $"Conversation with ID {id} not found." });
            }

            // Actualiza solo los campos permitidos
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
        /// <param name="id">ID de la conversación a eliminar.</param>
        /// <returns>Resultado de la eliminación.</returns>
        /// <response code="204">Conversación eliminada correctamente.</response>
        /// <response code="404">Si la conversación no existe.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteConversations")]
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
