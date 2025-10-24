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
using System.Linq;
using Voia.Api.Services;
using Voia.Api.Services.Chat;
using Voia.Api.Services.Interfaces;
using Voia.Api.Attributes;
using Voia.Api.Models.Users;
using Voia.Api.Models;


namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ConversationsController : ControllerBase
    {
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly BotDataCaptureService _captureService;
    private readonly PromptBuilderService _promptBuilder;
    private readonly IAiProviderService _aiProviderService;
    private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext,
            BotDataCaptureService captureService,
            PromptBuilderService promptBuilder,
            IAiProviderService aiProviderService,
            ILogger<ConversationsController> logger
        )
        {
            _context = context;
            _hubContext = hubContext;
            _captureService = captureService;
            _promptBuilder = promptBuilder;
            _aiProviderService = aiProviderService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las conversaciones con los datos relacionados de usuario y bot.
        /// </summary>
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
                    c.Status,
                    Title = c.Title ?? string.Empty,
                    UserMessage = c.UserMessage ?? string.Empty,
                    BotResponse = c.BotResponse ?? string.Empty,
                    IsWithAI = c.IsWithAI,
                    Alias = $"Sesión {c.Id}",
                    Bot = c.Bot != null ? new { c.Bot.Name } : null
                })
                .ToListAsync();

            return Ok(conversations);
        }

        [HttpPost("get-or-create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrGetConversation([FromBody] CreateConversationDto dto)
        {
            try
            {
                // Para usuarios anónimos del widget, usar botId del DTO directamente
                // (en lugar de validar con token por compatibilidad temporal)
                var validatedBotId = dto.BotId;

                // Verificar que el bot existe
                var botExists = await _context.Bots.AnyAsync(b => b.Id == validatedBotId);
                if (!botExists)
                {
                    return BadRequest("Bot no encontrado.");
                }

                // Para widgets, manejar usuarios públicos
                int? effectiveUserId = null;
                int? effectivePublicUserId = null;
                
                if (!dto.UserId.HasValue || dto.UserId <= 0)
                {
                    // Para usuarios del widget, buscar o crear en public_users
                    var request = HttpContext.Request;
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var userAgent = request.Headers.UserAgent.ToString();
                    
                    var publicUser = await _context.PublicUsers.FirstOrDefaultAsync(pu => 
                        pu.IpAddress == ipAddress && pu.BotId == dto.BotId);
                    
                    if (publicUser == null)
                    {
                        publicUser = new PublicUser
                        {
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            Country = "Unknown",
                            City = "Unknown",
                            BotId = dto.BotId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.PublicUsers.Add(publicUser);
                        await _context.SaveChangesAsync();
                    }
                    
                    effectivePublicUserId = publicUser.Id;
                    
                    // Para usuarios públicos del widget, necesitamos un userId dummy válido para el FK
                    // Buscar cualquier usuario existente para usar como referencia del FK
                    var existingUser = await _context.Users.Select(u => new { u.Id }).FirstOrDefaultAsync();
                    
                    if (existingUser == null)
                    {
                        // Si no hay usuarios, crear uno básico
                        var firstRole = await _context.Roles.FirstOrDefaultAsync();
                        if (firstRole == null)
                        {
                            // Crear role default si no existe ninguno
                            firstRole = new Role { Name = "Widget System" };
                            _context.Roles.Add(firstRole);
                            await _context.SaveChangesAsync();
                        }
                        
                        var systemUser = new User
                        {
                            Name = "Sistema Widget",
                            Email = "system@widget.local",
                            Password = "no-password",
                            Phone = "",
                            DocumentNumber = "",
                            DocumentPhotoUrl = "",
                            RoleId = firstRole.Id,
                            IsVerified = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Users.Add(systemUser);
                        await _context.SaveChangesAsync();
                        effectiveUserId = systemUser.Id;
                    }
                    else
                    {
                        effectiveUserId = existingUser.Id;
                    }
                }
                else
                {
                    effectiveUserId = dto.UserId.Value;
                }

                // Buscar conversación existente - para widgets usar PublicUserId, para usuarios regulares usar UserId
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => 
                        (effectivePublicUserId.HasValue ? c.PublicUserId == effectivePublicUserId : c.UserId == effectiveUserId) && 
                        c.BotId == dto.BotId);

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        UserId = effectiveUserId.Value,
                        PublicUserId = effectivePublicUserId,
                        BotId = dto.BotId,
                        Title = effectivePublicUserId.HasValue ? "Widget Chat" : "Chat con bot",
                        CreatedAt = DateTime.UtcNow,
                        Status = "active",
                        IsWithAI = true
                    };

                    _context.Conversations.Add(conversation);
                    await _context.SaveChangesAsync();

                    var conversationDto = new
                    {
                        id = conversation.Id,
                        alias = $"Sesión {conversation.Id}",
                        lastMessage = "",
                        updatedAt = conversation.UpdatedAt,
                        status = conversation.Status,
                        blocked = conversation.Blocked,
                        isWithAI = conversation.IsWithAI,
                        unreadCount = 0,
                        isWidget = effectivePublicUserId.HasValue,
                        publicUserId = effectivePublicUserId
                    };

                    // Notificar al panel de admin sobre la nueva conversación
                    // Incluimos las conversaciones creadas por widgets (public users)
                    await _hubContext.Clients.Group("admin").SendAsync("NewConversation", conversationDto);
                }

                return Ok(new { conversationId = conversation.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        public class CreateConversationDto
        {
            public int? UserId { get; set; }
            public int BotId { get; set; }
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
    [HasPermission("CanViewConversations")]
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
                        IsWithAI = c.IsWithAI,
                        Alias = $"Sesión {c.Id}",
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

        [HttpGet("history/{conversationId}")]
    [HasPermission("CanViewConversations")]
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
                .Include(m => m.PublicUser)
                .Include(m => m.Bot)
                .Select(m => new ConversationItemDto
                {
                    Id = m.Id,
                    Type = "message",
                    Text = m.MessageText,
                    Timestamp = m.CreatedAt,
                    FromRole = m.Sender,
                    FromId = m.Sender == "user" ? (m.UserId ?? m.PublicUserId) : m.BotId,
                    FromName = m.Sender == "user"
        ? (m.User != null ? m.User.Name : m.PublicUser != null ? $"Visitante {m.PublicUser.Id}" : "Usuario")
        : (m.Bot != null ? m.Bot.Name : "Bot"),
                    FromAvatarUrl = m.Sender == "user"
        ? (m.User != null ? (m.User.AvatarUrl ?? "") : "")
        : "https://yourdomain.com/images/default-bot-avatar.png",
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
                    Type = f.FileType.StartsWith("image") ? "image" : "file", // 👈 diferenciar tipo
                    Timestamp = f.UploadedAt ?? DateTime.UtcNow,
                    FromRole = "user",
                    FromId = f.UserId,
                    FromName = f.User != null ? f.User.Name : "Usuario",
                    FromAvatarUrl = f.User != null ? (f.User.AvatarUrl ?? "") : "",
                    FileUrl = f.FilePath,
                    FileName = f.FileName,
                    FileType = f.FileType
                })
                .ToListAsync();

            _logger?.LogInformation("[GetConversationHistory] Conversation {ConversationId}: messages={MessageCount}, files={FileCount}", conversationId, messages.Count, files.Count);

                // DEBUG: log a small sample of messages returned to help diagnose missing public-user messages
                try
                {
                    var sample = messages.Take(10).Select(m => new { m.Id, m.Type, m.Timestamp, m.FromRole, m.FromId, m.FromName, Text = m.Text ?? "" });
                    _logger?.LogInformation("[GetConversationHistory] sample messages for conv {ConversationId}: {Sample}", conversationId, System.Text.Json.JsonSerializer.Serialize(sample));
                }
                catch { /* ignore logging errors */ }

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
                    status = conversation.Status,
                    isWithAI = conversation.IsWithAI
                },
                history = combinedHistory
            });
        }

        [HttpGet("with-last-message")]
    [HasPermission("CanViewConversations")]
    public async Task<IActionResult> GetConversationsWithLastMessage()
        {
            var conversations = await _context.Conversations
                .Include(c => c.Bot) // Make sure Bot is included
                .Select(c => new
                {
                    Conversation = c,
                    LastEvent = _context.Messages
                        .Where(m => m.ConversationId == c.Id)
                        .Select(m => new { RawContent = m.MessageText, Timestamp = (DateTime?)m.CreatedAt, Type = "text" })
                        .Concat(_context.ChatUploadedFiles
                            .Where(f => f.ConversationId == c.Id)
                            .Select(f => new { RawContent = f.FileName, Timestamp = f.UploadedAt, Type = f.FileType.StartsWith("image") ? "image" : "file" })
                        )
                        .OrderByDescending(e => e.Timestamp)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = conversations.Select(c =>
            {
                string? finalContent = c.LastEvent?.RawContent;
                if (c.LastEvent?.Type == "text" && !string.IsNullOrEmpty(finalContent) && finalContent.Trim().StartsWith("{"))
                {
                    try
                    {
                        using (var doc = System.Text.Json.JsonDocument.Parse(finalContent))
                        {
                            if (doc.RootElement.TryGetProperty("UserQuestion", out var userQuestion))
                            {
                                finalContent = userQuestion.GetString() ?? finalContent;
                            }
                            else if (doc.RootElement.TryGetProperty("Content", out var content))
                            {
                                finalContent = content.GetString() ?? finalContent;
                            }
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Not a valid JSON, so we'll just use the original RawContent
                    }
                }

                return new
                {
                    c.Conversation.Id,
                    c.Conversation.Status,
                    Title = c.Conversation.Title ?? string.Empty,
                    c.Conversation.IsWithAI,
                    Alias = $"Sesión {c.Conversation.Id}",
                    Bot = c.Conversation.Bot != null ? new { c.Conversation.Bot.Name } : null,
                    lastMessage = c.LastEvent == null ? null : new
                    {
                        c.LastEvent.Type,
                        Content = finalContent,
                        Timestamp = c.LastEvent.Timestamp ?? c.Conversation.UpdatedAt
                    }
                };
            });

            return Ok(result);
        }


        /// <summary>
        /// Actualiza el estado de una conversación específica.
        /// </summary>
        [HttpPatch("{id}/status")]
    [HasPermission("CanEditConversations")]
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
    [HasPermission("CanEditConversations")]
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
            conversation.IsWithAI = dto.IsWithAI; // ← solo si quieres que pueda modificarse desde el frontend

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return Ok(conversation);
        }

        /// <summary>
        /// Elimina una conversación por su ID.
        /// </summary>
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
        public class UpdateIsWithAIDto
        {
            public bool IsWithAI { get; set; }
        }

        [HttpPatch("{id}/with-ai")]
        public async Task<IActionResult> UpdateIsWithAI(int id, [FromBody] UpdateIsWithAIDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
            {
                return NotFound(new { message = $"Conversación con ID {id} no encontrada." });
            }

            conversation.IsWithAI = dto.IsWithAI;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Campo isWithAI actualizado a {dto.IsWithAI} para la conversación {id}." });
        }

        /// <summary>
        /// Envía un mensaje en una conversación y procesa captura de datos + prompt.
        /// </summary>
        [HttpPost("{conversationId}/send")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] UserMessageDto request)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Bot)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return NotFound("Conversación no encontrada");

            // 1. Capturar datos del mensaje
            var newSubmissions = await _captureService.ProcessMessageAsync(
                conversation.BotId,
                conversation.UserId,
                conversationId.ToString(),
                request.Message,
                new List<DataField>() // Se pasa una lista vacía porque este endpoint no mantiene estado de campos.
            );

            // 2. Traer todos los campos con sus valores actuales
            var capturedFields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == conversation.BotId)
                .Select(f => new DataField
                {
                    FieldName = f.FieldName,
                    // 🔹 CORRECCIÓN: Usamos el conversationId como identificador de sesión único para aislar los datos por visitante.
                    Value = _context.BotDataSubmissions.Where(s =>
                            s.BotId == conversation.BotId && s.CaptureFieldId == f.Id && s.SubmissionSessionId == conversationId.ToString())
                        .OrderByDescending(s => s.SubmittedAt) // Tomamos el más reciente para esta sesión
                        .Select(s => s.SubmissionValue)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 3. Construir el prompt y obtener respuesta del bot (mock o IA real)
            string finalPrompt = await _promptBuilder.BuildPromptFromBotContextAsync(
                conversation.BotId,
                conversation.UserId, // 👈 AÑADIDO: Pasamos el UserId de la conversación
                request.Message, // Mensaje del usuario
                capturedFields
            );

            string botResponse = await _aiProviderService.GetBotResponseAsync(
                conversation.BotId,
                conversation.UserId,
                finalPrompt, // Usamos el prompt completo que construimos
                capturedFields
            );

            // 4. Guardar mensaje en DB
            var message = new Message
            {
                ConversationId = conversationId,
                Sender = "ai",
                BotId = conversation.BotId,
                MessageText = botResponse,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 5. Enviar mensaje al frontend vía SignalR
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "ai",
                    text = botResponse,
                    id = message.Id.ToString(),
                    timestamp = message.CreatedAt
                });

            // 6. Retornar ok al POST original
            return Ok(new
            {
                captured = newSubmissions.NewSubmissions.Select(s => new { FieldName = s.CaptureField?.FieldName, s.SubmissionValue }),
                botResponse
            });
        }

        // DTOs internos
        public class UserMessageDto
        {
            public required string Message { get; set; }
        }

        public class UpdateStatusDto
        {
            public required string Status { get; set; }
        }
    }
}