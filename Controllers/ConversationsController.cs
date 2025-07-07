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
        [HasPermission("CanViewConversations")]
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

        /// <summary>
        /// Crea una nueva conversación.
        /// </summary>
        /// <param name="dto">Datos necesarios para crear la conversación.</param>
        /// <returns>La conversación creada.</returns>
        /// <response code="201">Devuelve la conversación recién creada.</response>
        /// <response code="404">Si el usuario o el bot no existen.</response>
        [HttpPost]
      //  [HasPermission("CanCreateConversations")]
        public async Task<ActionResult<Conversation>> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {dto.UserId} not found." });
            }

            var bot = await _context.Bots.FindAsync(dto.BotId);
            if (bot == null)
            {
                return NotFound(new { message = $"Bot with ID {dto.BotId} not found." });
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
