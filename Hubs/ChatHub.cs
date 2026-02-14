using Microsoft.AspNetCore.SignalR;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.Conversations;
using Voia.Api.Data;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Voia.Api.Models.Messages;
using Voia.Api.Models.Chat;
using System.IO;
using Voia.Api.Services.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Api.Services;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Voia.Api.Services;
using Voia.Api.Models.Users;
using Voia.Api.Models.Messages.DTOs;
using Microsoft.AspNetCore.Authorization; // ✅ Para [Authorize] y [AllowAnonymous]

namespace Voia.Api.Hubs
{
    [Authorize] // ✅ Habilitado para que Context.User se popule con el token JWT
    public class ChatHub : Hub
    {
        // Validación manual de JWT para SignalR (acepta tokens del widget)
        private bool ValidateWidgetJwt(string token, out string botId)
        {
            botId = null;
            try
            {
                _logger.LogInformation($"[DEBUG][ValidateWidgetJwt] Token recibido: {token}");
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = Context.GetHttpContext()?.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                var jwtKey = key?["Jwt:Key"];
                var jwtIssuer = key?["Jwt:Issuer"];
                var jwtAudience = key?["Jwt:Audience"];
                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
                };
                // Intentar validación estándar
                try
                {
                    var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
                    var botIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "botId");
                    if (botIdClaim != null)
                    {
                        botId = botIdClaim.Value;
                        _logger.LogInformation($"[DEBUG][ValidateWidgetJwt] Validación estándar exitosa. botId: {botId}");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Validación estándar: botId claim no encontrado.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Excepción en validación estándar: {ex.Message}");
                    // Fallback: leer sin validar firma
                    try
                    {
                        var jwt = handler.ReadJwtToken(token);
                        var botIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "botId");
                        var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                        if (botIdClaim != null && expClaim != null)
                        {
                            botId = botIdClaim.Value;
                            var expUnix = long.TryParse(expClaim.Value, out var expVal) ? expVal : 0;
                            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            if (expUnix > nowUnix)
                            {
                                _logger.LogInformation($"[DEBUG][ValidateWidgetJwt] Fallback exitosa. botId: {botId}, exp: {expUnix}, now: {nowUnix}");
                                return true;
                            }
                            else
                            {
                                _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Fallback: token expirado. exp: {expUnix}, now: {nowUnix}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Fallback: botId o exp claim no encontrado.");
                        }
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Excepción en fallback: {ex2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Excepción general: {ex.Message}");
            }
            _logger.LogWarning($"[DEBUG][ValidateWidgetJwt] Token inválido para widget.");
            return false;
        }
        private readonly IAiProviderService _aiProviderService;
        private readonly ApplicationDbContext _context;
        private readonly IChatFileService _chatFileService;
        private readonly ILogger<ChatHub> _logger;
        private readonly TokenCounterService _tokenCounter;
        private readonly BotDataCaptureService _dataCaptureService;
        private readonly HttpClient _httpClient; // <--- HttpClient inyectado
        private readonly Voia.Api.Services.Upload.IFileSignatureChecker _checker;
        private const int TypingDelayMs = 1000; // 1 segundo
        private readonly PromptBuilderService _promptBuilderService;


        // Optional presence service - may be null if Redis not configured
        private readonly PresenceService? _presenceService;
        private readonly IMessageQueue? _messageQueue;
        
        // Email notifications for offline admins
        private readonly IEmailService _emailService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> _userManager;

        public ChatHub(
            IAiProviderService aiProviderService,
            ApplicationDbContext context,
            IChatFileService chatFileService,
            ILogger<ChatHub> logger,
            TokenCounterService tokenCounter,
            BotDataCaptureService dataCaptureService,
            HttpClient httpClient,
            PromptBuilderService promptBuilderService,
            Voia.Api.Services.Upload.IFileSignatureChecker checker,
            IEmailService emailService,
            Microsoft.AspNetCore.Identity.UserManager<User> userManager,
            PresenceService? presenceService = null,
            IMessageQueue? messageQueue = null
        )
        {
            _aiProviderService = aiProviderService;
            _context = context;
            _chatFileService = chatFileService;
            _logger = logger;
            _tokenCounter = tokenCounter;
            _dataCaptureService = dataCaptureService;
            _httpClient = httpClient;
            _promptBuilderService = promptBuilderService;
            _checker = checker;
            _presenceService = presenceService;
            _messageQueue = messageQueue;
            _emailService = emailService;
            _userManager = userManager;
        }

        [AllowAnonymous] // ✅ Permitir acceso anónimo para validar tokens del widget manualmente
        public async Task JoinRoom(int conversationId)
        {
            if (conversationId <= 0)
            {
                throw new HubException("El ID de conversación debe ser un número positivo.");
            }

            // ✅ Verificar si es un usuario autenticado (admin/dashboard)
            var isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false;
            
            if (!isAuthenticated)
            {
                // Validar JWT manualmente si el usuario es anónimo (widget)
                var httpContext = Context.GetHttpContext();
                var token = httpContext?.Request.Query["access_token"].FirstOrDefault();
                string? botIdFromToken = null;
                if (!string.IsNullOrEmpty(token))
                {
                    if (!ValidateWidgetJwt(token, out botIdFromToken))
                    {
                        throw new HubException("Token JWT inválido para widget.");
                    }
                    _logger.LogInformation("✅ [JoinRoom] Usuario widget validado para conversación {conv}", conversationId);
                }
                else
                {
                    _logger.LogWarning("⚠️ [JoinRoom] Usuario no autenticado sin token JWT intentando unirse a conversación {conv}", conversationId);
                }
            }
            else
            {
                var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("✅ [JoinRoom] Usuario autenticado {userId} uniéndose a conversación {conv}", userId, conversationId);
            }

            try
            {
                _logger.LogInformation("🚪 [JoinRoom] Intentando unir ConnectionId '{connId}' al grupo '{group}'", Context.ConnectionId, conversationId.ToString());
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
                _logger.LogInformation("✅ [JoinRoom] ConnectionId '{connId}' unido exitosamente al grupo '{group}'", Context.ConnectionId, conversationId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [JoinRoom] Error al unir ConnectionId '{connId}' al grupo '{group}'", Context.ConnectionId, conversationId.ToString());
                _logger.LogError(ex, "❌ Error en JoinRoom.");
                throw;
            }
        }

        public async Task LeaveRoom(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task JoinAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
            await SendInitialConversations();
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && _presenceService != null)
                {
                    await _presenceService.AddConnectionAsync(userId, Context.ConnectionId);
                }
                _logger.LogInformation("SignalR connected: {conn} user:{user}", Context.ConnectionId, userId);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error in OnConnectedAsync");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && _presenceService != null)
                {
                    await _presenceService.RemoveConnectionAsync(userId, Context.ConnectionId);
                }
                _logger.LogInformation("SignalR disconnected: {conn} user:{user} ex:{ex}", Context.ConnectionId, userId, exception?.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error in OnDisconnectedAsync");
            }
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendInitialConversations()
        {
            var conversationsData = await _context.Conversations
                .Include(c => c.Bot)
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

            var result = conversationsData.Select(c =>
            {
                string lastMessageString = "Conversación iniciada";
                if (c.LastEvent != null)
                {
                    // Use raw content as-is (parsing JSON here caused nullable conversion issues).
                    string finalContent = c.LastEvent.RawContent ?? string.Empty;

                    lastMessageString = c.LastEvent.Type switch
                    {
                        "text" => finalContent ?? string.Empty,
                        "image" => "📷 Imagen",
                        "file" => $"📎 Archivo: {finalContent ?? string.Empty}",
                        _ => finalContent ?? string.Empty
                    };
                }

                return new
                {
                    id = c.Conversation.Id,
                    alias = $"Sesión {c.Conversation.Id}",
                    status = c.Conversation.Status,
                    isWithAI = c.Conversation.IsWithAI,
                    blocked = c.Conversation.Blocked,
                    lastMessage = lastMessageString,
                    updatedAt = c.LastEvent?.Timestamp ?? c.Conversation.UpdatedAt
                };
            }).ToList();

            await Clients.Caller.SendAsync("InitialConversations", result);
        }
        [AllowAnonymous] // ✅ Widget puede notificar actividad
        public async Task UserIsActive(int conversationId)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                bool statusChanged = false;
                if (conversation.Status == "inactiva")
                {
                    conversation.Status = "activa";
                    statusChanged = true; // El estado cambió
                }

                conversation.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // ✅ Si el estado cambió, notificar al panel de admin
                if (statusChanged)
                {
                    await Clients.Group("admin").SendAsync("ConversationStatusChanged", conversation.Id, "activa");
                }
                await Clients.Group("admin").SendAsync("Heartbeat", conversationId);
            }
        }
        [AllowAnonymous] // ✅ Permitir que el widget cree conversaciones sin autenticación
        public async Task<int> InitializeContext(object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int botId = root.GetProperty("botId").GetInt32();
                int userId = root.GetProperty("userId").GetInt32();

                var existing = _context.Conversations
                    .FirstOrDefault(c => c.UserId == userId && c.BotId == botId);

                if (existing != null)
                {
                    return existing.Id;
                }

                var newConversation = new Conversation
                {
                    BotId = botId,
                    PublicUserId = userId,
                    Title = "Nueva conversación",
                    CreatedAt = DateTime.UtcNow,
                    Status = "activa"
                };

                _context.Conversations.Add(newConversation);
                await _context.SaveChangesAsync();

                // Notificar al grupo admin sobre la nueva conversación
                _logger.LogInformation("📢 [ChatHub] Enviando NewConversation a grupo admin desde InitializeContext para ConversationId: {ConversationId}", newConversation.Id);
                await Clients.Group("admin").SendAsync("NewConversation", new
                {
                    id = newConversation.Id,
                    alias = $"Sesión {newConversation.Id}",
                    lastMessage = newConversation.UserMessage, // UserMessage podría ser null aquí para una nueva conversación
                    updatedAt = newConversation.UpdatedAt,
                    status = newConversation.Status,
                    blocked = newConversation.Blocked,
                    isWithAI = newConversation.IsWithAI // Added IsWithAI
                });

                await Clients.Group("admin").SendAsync("Heartbeat", newConversation.Id);

                return newConversation.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en InitializeContext."); // Cambiado a _logger.LogError
                throw new HubException("No se pudo inicializar la conversación.");
            }
        }

        [AllowAnonymous] // ✅ Widget puede notificar expiración
        public async Task NotifyWidgetExpired(int conversationId)
        {
            try
            {
                _logger.LogInformation("🖥️ [Hub] Widget expiró/cerró conversación {convId}", conversationId);

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning("⚠️ [Hub] Conversación {convId} no encontrada", conversationId);
                    return;
                }

                // Marcar como cerrada y expirada
                conversation.Status = "expired";
                conversation.ClosedAt = DateTime.UtcNow;
                conversation.ExpiresAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ [Hub] Widget cerró/expiró conversación {convId}", conversationId);

                // Notificar a TODOS en el grupo (incluyendo admin)
                await Clients.Group(conversationId.ToString())
                    .SendAsync("WidgetSessionEnded", new
                    {
                        conversationId,
                        reason = "widget-expired",
                        closedAt = conversation.ClosedAt,
                        expiresAt = conversation.ExpiresAt
                    });

                await Clients.Group("admin").SendAsync("ConversationStatusChanged", conversationId, "expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [NotifyWidgetExpired] Error: {msg}", ex.Message);
            }
        }
        // ✅ Método para pausar o activar la IA desde el admin

        [AllowAnonymous] // ✅ Widget puede enviar mensajes sin autenticación JWT de usuario
        public async Task SendMessage(int conversationId, AskBotRequestDto request)
        {
            // LOG explícito: registro de payload y contexto de conexión
            _logger.LogInformation($"[SignalR][SendMessage][INICIO] Payload recibido: {System.Text.Json.JsonSerializer.Serialize(request)}");
            _logger.LogInformation($"[SignalR][SendMessage][INICIO] Contexto: ConnectionId={Context.ConnectionId}, User={Context.User?.Identity?.Name}, ConversationId={conversationId}");
            try
            {
                // ✅ VALIDACIÓN DE DTO (Cumplir con DataAnnotations)
                if (request == null)
                {
                    _logger.LogError("❌ [SendMessage] Request es nulo");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: solicitud inválida.",
                        status = "error",
                        tempId = request?.TempId
                    });
                    return;
                }

                // Validar Question (Required)
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    _logger.LogError("❌ [SendMessage] Pregunta vacía o nula");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: La pregunta es requerida.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                // Validar Question length (1-5000 caracteres)
                if (request.Question.Length < 1 || request.Question.Length > 5000)
                {
                    _logger.LogError($"❌ [SendMessage] Pregunta inválida - Longitud: {request.Question.Length}");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: La pregunta debe tener entre 1 y 5000 caracteres.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                // Validar Question - No permitir <, >, {, }
                if (request.Question.Contains("<") || request.Question.Contains(">") ||
                    request.Question.Contains("{") || request.Question.Contains("}"))
                {
                    _logger.LogError($"❌ [SendMessage] Pregunta contiene caracteres prohibidos: {request.Question}");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: El mensaje no puede contener caracteres '<', '>', '{' o '}'.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                _logger.LogInformation($"🔍 [SendMessage] Iniciando - ConversationId: {conversationId}, BotId: {request.BotId}, UserId: {request.UserId}, Question: {request.Question}");

                // Note: AI processing moved to background worker. Hub will persist message and enqueue a job.

                // 1. Obtener la conversación existente
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    _logger.LogError($"❌ [SendMessage] Conversación {conversationId} no encontrada");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: conversación no encontrada.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                _logger.LogInformation($"✅ [SendMessage] Conversación encontrada - PublicUserId: {conversation.PublicUserId}, UserId: {conversation.UserId}");

                // 2. Validar que el bot coincida
                if (conversation.BotId != request.BotId)
                {
                    _logger.LogError($"❌ [SendMessage] BotId no coincide - Esperado: {conversation.BotId}, Recibido: {request.BotId}");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: bot no coincide con la conversación.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                // 3. Asegurar que el cliente esté en el grupo de la conversación
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

                // 4. Obtener mensaje al que se responde (si aplica)
                string? repliedText = null;
                if (request.ReplyToMessageId.HasValue)
                {
                    repliedText = await _context.Messages
                        .Where(m => m.Id == request.ReplyToMessageId.Value)
                        .Select(m => m.MessageText)
                        .FirstOrDefaultAsync();
                }

                // LOG DETALLADO: Estado de la conversación y DTO recibido
                _logger.LogInformation($"[DEBUG][SendMessage] DTO recibido: {{ botId: {request.BotId}, userId: {request.UserId}, question: '{request.Question}', tempId: {request.TempId} }}");
                _logger.LogInformation($"[DEBUG][SendMessage] Estado de la conversación: Id={conversation?.Id}, PublicUserId={conversation?.PublicUserId}, UserId={conversation?.UserId}, Status={conversation?.Status}");

                // 5. Determinar el PublicUserId apropiado para el mensaje
                int? messagePublicUserId = null;

                if (conversation.PublicUserId.HasValue)
                {
                    // Es una conversación de widget anónimo
                    messagePublicUserId = conversation.PublicUserId.Value;
                    _logger.LogInformation($"🔍 [SendMessage] Usando PublicUserId para mensaje: {messagePublicUserId}");
                }
                else if (request.UserId.HasValue)
                {
                    // Es una conversación de usuario autenticado
                    messagePublicUserId = request.UserId.Value;
                    _logger.LogInformation($"🔍 [SendMessage] Usando UserId para mensaje: {messagePublicUserId}");
                }
                else
                {
                    _logger.LogError($"❌ [SendMessage] No se pudo determinar el usuario para el mensaje | DTO: {{ botId: {request.BotId}, userId: {request.UserId}, question: '{request.Question}', tempId: {request.TempId} }} | Conversación: Id={conversation?.Id}, PublicUserId={conversation?.PublicUserId}, UserId={conversation?.UserId}, Status={conversation?.Status}");
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Error: no se pudo identificar el usuario.",
                        status = "error",
                        tempId = request.TempId
                    });
                    return;
                }

                // 6. Guardar mensaje del usuario
                var userMessage = new Message
                {
                    BotId = request.BotId,
                    UserId = conversation.PublicUserId.HasValue ? null : request.UserId, // Solo para usuarios autenticados
                    PublicUserId = conversation.PublicUserId, // Para usuarios anónimos de widget
                    ConversationId = conversation.Id,
                    Sender = "user",
                    MessageText = request.Question ?? string.Empty,
                    Source = string.IsNullOrWhiteSpace(request.Origen) ? "widget" : request.Origen.ToLower(),
                    CreatedAt = DateTime.UtcNow,
                    ReplyToMessageId = request.ReplyToMessageId
                };
                _logger.LogInformation($"[SendMessage] Intentando guardar mensaje del usuario: ConversationId={conversation.Id}, PublicUserId={userMessage.PublicUserId}, UserId={userMessage.UserId}, Texto='{userMessage.MessageText}'");
                _context.Messages.Add(userMessage);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID del mensaje
                _logger.LogInformation($"✅ [SendMessage] Mensaje de usuario guardado en DB: MessageId={userMessage.Id}, Sender={userMessage.Sender}, Texto='{userMessage.MessageText}'");

                // 🆕 Actualizar campos de la conversación
                if (string.IsNullOrEmpty(conversation.UserMessage))
                {
                    // Solo guardar el primer mensaje del usuario
                    conversation.UserMessage = request.Question ?? string.Empty;
                }
                conversation.LastMessage = request.Question ?? string.Empty;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ [SendMessage] Conversación actualizada - user_message (solo si vacío) y last_message");

                // Enviar mensaje del usuario al grupo para mostrarlo en el chat
                _logger.LogInformation("📤 [SendMessage] Enviando ReceiveMessage al grupo {conv} para mensaje {msgId}", conversationId, userMessage.Id);
                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    text = request.Question ?? string.Empty,
                    timestamp = userMessage.CreatedAt,
                    id = userMessage.Id,
                    tempId = request.TempId
                });
                _logger.LogInformation("✅ [SendMessage] ReceiveMessage enviado al grupo {conv} para mensaje {msgId}", conversationId, userMessage.Id);

                // 7. Contar tokens de usuario (solo para usuarios autenticados)
                if (_tokenCounter != null && !string.IsNullOrWhiteSpace(request.Question) && request.UserId.HasValue)
                {
                    _context.TokenUsageLogs.Add(new TokenUsageLog
                    {
                        UserId = request.UserId.Value, // Solo para usuarios autenticados
                        BotId = request.BotId,
                        TokensUsed = _tokenCounter.CountTokens(request.Question),
                        UsageDate = DateTime.UtcNow
                    });
                }

                // 🔹 PASO 1: Obtener el estado actual de los campos capturados para esta conversación
                var currentCapturedFields = await (from f in _context.BotDataCaptureFields
                                                   where f.BotId == request.BotId
                                                   select new DataField
                                                   {
                                                       FieldName = f.FieldName,
                                                       Value = string.Join(", ", _context.BotDataSubmissions.Where(s =>
                                                               s.BotId == request.BotId && s.CaptureFieldId == f.Id &&
                                                               s.SubmissionSessionId == conversationId.ToString())
                                                           .OrderBy(s => s.SubmittedAt)
                                                           .Select(s => s.SubmissionValue)
                                                           .Distinct())
                                                   })
                    .ToListAsync();

                _logger.LogInformation("🔹 [ChatHub] Campos cargados de BD: {count} campos", currentCapturedFields.Count);
                foreach (var field in currentCapturedFields)
                {
                    _logger.LogInformation("  - {fieldName}: {value}", field.FieldName, field.Value ?? "NULL");
                }

                // 🔹 PASO 2: Procesar el mensaje para capturar nuevos datos
                _logger.LogInformation("🔍 [ChatHub] Procesando mensaje con BotDataCaptureService: '{msg}'", request.Question);

                // 🆕 Construir historial de conversación para PHASE 3 (revisión retrospectiva)
                var conversationMessages = await _context.Messages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Sender, m.MessageText, m.CreatedAt })
                    .ToListAsync();

                var conversationHistory = string.Join("\n", conversationMessages.Select(m =>
                    $"[{m.Sender}]: {m.MessageText}"));

                _logger.LogInformation("📜 [ChatHub] Historial de conversación ({msgCount} mensajes): {preview}...",
                    conversationMessages.Count,
                    conversationHistory.Length > 200 ? conversationHistory.Substring(0, 200) : conversationHistory);

                var captureResult = await _dataCaptureService.ProcessMessageAsync(
                    request.BotId,
                    request.UserId,
                    conversationId.ToString(), // Usamos el ID de conversación como ID de sesión
                    request.Question ?? string.Empty,
                    currentCapturedFields, // ✅ FIX: Pasamos la lista de campos que obtuvimos
                    conversationHistory   // 🆕 PHASE 3: Pasamos el historial completo de conversación
                );

                _logger.LogInformation("📊 [ChatHub] Resultado de captura: {newSubmissions} nuevos, RequiresAiClarification: {requiresClarification}",
                    captureResult.NewSubmissions.Count, captureResult.RequiresAiClarification);

                // 🆕 Actualizar campos con datos capturados
                if (captureResult.NewSubmissions.Any())
                {
                    _logger.LogInformation("✅ [ChatHub] Se capturaron {count} nuevos datos en la captura de datos", captureResult.NewSubmissions.Count);
                    foreach (var submission in captureResult.NewSubmissions)
                    {
                        _logger.LogInformation("  📝 Submission: CaptureFieldId={id}, SubmissionValue='{value}'",
                            submission.CaptureFieldId, submission.SubmissionValue);

                        var field = currentCapturedFields.FirstOrDefault(f => f.FieldName == submission.CaptureField?.FieldName);
                        if (field != null)
                        {
                            field.Value = submission.SubmissionValue;
                            _logger.LogInformation("✅ [ChatHub] Campo actualizado: {fieldName} = {value}", field.FieldName, submission.SubmissionValue);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ [ChatHub] No se encontró campo en memoria para: {fieldName}", submission.CaptureField?.FieldName);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ [ChatHub] No se capturaron nuevos datos");
                }

                if (captureResult.RequiresAiClarification)
                {
                    string clarificationQuestion = await _aiProviderService.GetBotResponseAsync(
                        request.BotId,
                        request.UserId ?? 0, // Usar 0 para usuarios anónimos
                        captureResult.AiClarificationPrompt,
                        currentCapturedFields
                    );
                    // Console.WriteLine($"[ChatHub] Sending ReceiveMessage for grouped images. ConversationId: {conversationId}"); // Eliminado

                    await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = clarificationQuestion,
                        timestamp = DateTime.UtcNow
                    });
                    return; // Detener el procesamiento para esperar la respuesta del usuario
                }

                // Si el servicio de captura generó un mensaje de confirmación, encolamos un job para que el worker lo procese.
                if (!string.IsNullOrEmpty(captureResult.ConfirmationPrompt))
                {
                    if (_messageQueue != null)
                    {
                        var job = new MessageJob
                        {
                            ConversationId = conversation.Id,
                            BotId = request.BotId,
                            UserId = request.UserId,
                            MessageId = userMessage.Id,
                            Question = captureResult.ConfirmationPrompt,
                            TempId = request.TempId ?? string.Empty,
                            CapturedFields = currentCapturedFields // 🆕 AGREGAR CAMPOS CAPTURADOS AL JOB
                        };
                        await _messageQueue.EnqueueAsync(job);
                        await Clients.Caller.SendAsync("MessageQueued", new { conversationId, messageId = userMessage.Id, tempId = request.TempId });
                        return;
                    }
                }

                if (captureResult.NewSubmissions.Any())
                {
                    // Se eliminó la línea problemática: string.Join(...);
                    // Si se pretendía registrar esto, añade aquí la llamada a _logger.LogInformation.
                }

                // Actualizar conversación
                conversation.UserMessage = request.Question;
                conversation.LastMessage = request.Question;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Confirmación al usuario y notificaciones a grupo y admin
                _logger.LogInformation("📤 [SendMessage] Enviando ReceiveMessage (caller) para confirmación mensaje {msgId} conv {conv}", userMessage.Id, conversationId);
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    text = request.Question,
                    timestamp = userMessage.CreatedAt,
                    replyToMessageId = request.ReplyToMessageId,
                    replyToText = repliedText,
                    id = userMessage.Id,
                    tempId = request.TempId,
                    status = "sent"
                });
                _logger.LogInformation("✅ [SendMessage] ReceiveMessage (caller) enviado para mensaje {msgId} conv {conv}", userMessage.Id, conversationId);

                await Clients.OthersInGroup(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    text = request.Question,
                    timestamp = userMessage.CreatedAt,
                    replyToMessageId = request.ReplyToMessageId,
                    replyToText = repliedText,
                    id = userMessage.Id
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "user",
                    text = request.Question,
                    timestamp = userMessage.CreatedAt,
                    alias = $"Sesión {conversation.Id}",
                    lastMessage = request.Question,
                    replyToMessageId = request.ReplyToMessageId,
                    replyToText = repliedText,
                    id = userMessage.Id
                });

                // 📧 Verificar si admin está offline y enviar notificación por email
                await NotifyAdminIfOfflineAsync(conversationId, request.Question ?? string.Empty);

                await StopTyping(conversationId, "user");

                // Enqueue message for background processing (AI) to keep Hub responsive
                try
                {
                    // Si no hay message queue, fallback inmediato
                    if (_messageQueue == null)
                    {
                        _logger.LogWarning("⚠️ [SendMessage] Message queue no disponible, enviando fallback para conv {conv}", conversationId);
                        _logger.LogInformation("📤 [SendMessage] Intentando enviar fallback al grupo '{group}'", conversationId.ToString());
                        await SendFallbackBotMessage(conversation, request?.TempId);
                        _logger.LogInformation("✅ [SendMessage] Fallback enviado exitosamente");
                        return;
                    }

                    // Encolar mensaje para procesamiento por worker (IA)
                    var job = new MessageJob
                    {
                        ConversationId = conversation.Id,
                        BotId = request.BotId,
                        UserId = request.UserId,
                        MessageId = userMessage.Id,
                        Question = request.Question,
                        TempId = request.TempId ?? string.Empty,
                        CapturedFields = currentCapturedFields,
                        // 🌍 Pasar ubicación del usuario al Worker
                        UserCountry = request.UserLocation?.Country,
                        UserCity = request.UserLocation?.City,
                        ContextMessage = null // Opcional: agregar contexto adicional
                    };
                    
                    _logger.LogInformation("📬 [SendMessage] Encolando mensaje para procesamiento IA - conv {conv}, msg {msgId}", conversationId, userMessage.Id);
                    await _messageQueue.EnqueueAsync(job);
                    _logger.LogInformation("📤 [SendMessage] Enviando MessageQueued a Caller - conv {conv}, msg {msgId}, tempId {tempId}", conversationId, userMessage.Id, request.TempId);
                    await Clients.Caller.SendAsync("MessageQueued", new { conversationId, messageId = userMessage.Id, tempId = request.TempId });
                    _logger.LogInformation("✅ [SendMessage] Mensaje encolado exitosamente y MessageQueued enviado para conv {conv}", conversationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue message job for conversation {conv}", conversationId);
                    // Fallback en caso de error en el procesamiento de IA
                    await SendFallbackBotMessage(conversation, request?.TempId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [SendMessage] Error general en conversación {conversationId}: {ex.Message}");
                _logger.LogError($"❌ [SendMessage] Stack trace: {ex.StackTrace}");

                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Error interno del servidor. Inténtalo más tarde.",
                    status = "error",
                    tempId = request.TempId
                });
            }
        }

        // Método auxiliar para enviar y guardar el mensaje de fallback IA
        private async Task SendFallbackBotMessage(Conversation conversation, string tempId)
        {
            var fallbackText = $"✅ Mensaje recibido correctamente: '{conversation.UserMessage ?? "sin texto"}'. El sistema de IA no está conectado aún, pero el flujo de mensajería funciona.";
            _logger.LogInformation("🤖 [SendFallbackBotMessage] Preparando mensaje de fallback para conv {conv}", conversation.Id);
            var botFallbackMessage = new Voia.Api.Models.Messages.Message
            {
                BotId = conversation.BotId,
                UserId = null,
                PublicUserId = conversation.PublicUserId,
                ConversationId = conversation.Id,
                Sender = "bot",
                MessageText = fallbackText,
                Source = "ai-fallback",
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(botFallbackMessage);
            await _context.SaveChangesAsync();
            _logger.LogInformation("💾 [SendFallbackBotMessage] Mensaje guardado en BD - ID: {msgId}", botFallbackMessage.Id);

            _logger.LogInformation("📤 [SendFallbackBotMessage] Enviando ReceiveMessage al grupo '{group}'", conversation.Id.ToString());
            await Clients.Group(conversation.Id.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId = conversation.Id,
                from = "bot",
                text = fallbackText,
                timestamp = botFallbackMessage.CreatedAt,
                id = botFallbackMessage.Id,
                status = "sent",
                tempId
            });
            _logger.LogInformation("✅ [SendFallbackBotMessage] ReceiveMessage enviado exitosamente al grupo");
            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId = conversation.Id,
                from = "bot",
                text = fallbackText,
                timestamp = botFallbackMessage.CreatedAt,
                id = botFallbackMessage.Id,
                alias = $"Sesión {conversation.Id}",
                lastMessage = fallbackText
            });
        }

        public async Task AdminMessage(int conversationId, string text, int? replyToMessageId = null, string? replyToText = null)
        {
            try
            {
                var convo = await _context.Conversations.FindAsync(conversationId);

                string? repliedText = null;

                if (replyToMessageId.HasValue)
                {
                    // Use async EF call to avoid blocking the DB context while other async
                    // operations (e.g., SaveChangesAsync) may be in flight.
                    repliedText = await _context.Messages
                        .Where(m => m.Id == replyToMessageId.Value)
                        .Select(m => m.MessageText)
                        .FirstOrDefaultAsync();
                }

                if (convo != null)
                {
                    var adminMessage = new Message
                    {
                        BotId = convo.BotId,
                        UserId = convo.UserId,
                        ConversationId = conversationId,
                        Sender = "admin",
                        MessageText = text,
                        Source = "admin-panel",
                        CreatedAt = DateTime.UtcNow,
                        ReplyToMessageId = replyToMessageId
                    };

                    _context.Messages.Add(adminMessage);
                    // Actualizar LastMessage en la conversación
                    convo.LastMessage = text;
                    convo.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Recuperar el mensaje persistido para asegurar el id y timestamp reales
                var saved = await _context.Messages
                    .Where(m => m.ConversationId == conversationId && m.Sender == "admin")
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                var sendTimestamp = saved?.CreatedAt ?? DateTime.UtcNow;
                var sendId = saved?.Id;

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "admin",
                    text,
                    timestamp = sendTimestamp,
                    id = sendId,
                    replyToMessageId = replyToMessageId,
                    replyToText = repliedText
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "admin",
                    text,
                    timestamp = sendTimestamp,
                    id = sendId,
                    alias = $"Sesión {conversationId}",
                    lastMessage = text,
                    replyToMessageId = replyToMessageId,
                    replyToText = repliedText
                });

                await StopTyping(conversationId, "admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en AdminMessage para conversation {convId}: {msg}", conversationId, ex.Message);
                // Re-throw as HubException so the client receives a readable message and the stack is logged
                throw new HubException("Error al enviar mensaje desde el panel administrativo: " + ex.Message);
            }
        }

        /// <summary>
        /// 🆕 Guardar mensaje de bienvenida inicial del bot en la conversación
        /// Este mensaje se guarda directamente en la BD como respuesta del bot
        /// NO como entrada del usuario
        /// </summary>
        [AllowAnonymous] // ✅ Widget puede guardar mensaje de bienvenida
        [HubMethodName("SaveWelcomeMessage")]
        public async Task SaveWelcomeMessage(int conversationId, string welcomeText, int botId)
        {
            try
            {
                _logger.LogInformation($"💾 [SaveWelcomeMessage] Guardando mensaje de bienvenida - ConvId: {conversationId}, BotId: {botId}");

                // Validar que la conversación exista
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    _logger.LogError($"❌ [SaveWelcomeMessage] Conversación {conversationId} no encontrada");
                    throw new HubException("Conversación no encontrada");
                }

                // Validar que el botId coincida
                if (conversation.BotId != botId)
                {
                    _logger.LogError($"❌ [SaveWelcomeMessage] BotId no coincide - Esperado: {conversation.BotId}, Recibido: {botId}");
                    throw new HubException("Bot no coincide con la conversación");
                }

                // Agregar a grupo si no está
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

                // Guardar mensaje de bienvenida como respuesta del BOT (no del usuario)
                var welcomeMessage = new Message
                {
                    BotId = botId,
                    UserId = conversation.UserId, // Usuario autenticado si existe
                    PublicUserId = conversation.PublicUserId, // Usuario anónimo del widget
                    ConversationId = conversationId,
                    Sender = "bot", // 🔹 IMPORTANTE: Guardar como "bot", no "user"
                    MessageText = welcomeText ?? "👋 ¡Hola! Bienvenido. ¿En qué puedo ayudarte hoy?",
                    Source = "widget",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(welcomeMessage);
                // Actualizar BotResponse y LastMessage en la conversación
                conversation.BotResponse = welcomeMessage.MessageText;
                conversation.LastMessage = welcomeMessage.MessageText;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ [SaveWelcomeMessage] Mensaje de bienvenida guardado - MessageId: {welcomeMessage.Id}");

                // Notificar a los clientes en la conversación
                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    id = welcomeMessage.Id,
                    from = "bot",
                    text = welcomeMessage.MessageText,
                    timestamp = welcomeMessage.CreatedAt,
                    status = "sent"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en SaveWelcomeMessage para conversation {convId}: {msg}", conversationId, ex.Message);
                throw new HubException("Error al guardar mensaje de bienvenida: " + ex.Message);
            }
        }

        [AllowAnonymous] // ✅ Widget puede enviar imágenes
        [HubMethodName("SendGroupedImages")]
        public async Task SendGroupedImages(int conversationId, int? userId, List<ChatFileDto> multipleFiles)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            try
            {
                _logger.LogInformation($"📸 [SendGroupedImages] Iniciando. ConvId: {conversationId}, UserId: {userId}, FilesCount: {multipleFiles?.Count}");

                if (multipleFiles == null || multipleFiles.Count == 0)
                {
                    _logger.LogWarning($"⚠️ [SendGroupedImages] Sin archivos para procesar");
                    throw new HubException("No se recibieron archivos para procesar.");
                }

                var fileDtos = new List<object>();
                var messagesToAdd = new List<Message>();

                // Get conversation and bot once
                var convo = await _context.Conversations.FindAsync(conversationId);
                if (convo == null)
                {
                    _logger.LogWarning($"⚠️ [SendGroupedImages] Conversación no encontrada: {conversationId}");
                    throw new HubException($"Conversación {conversationId} no encontrada.");
                }

                var bot = await _context.Bots.FindAsync(convo.BotId);
                if (bot == null)
                {
                    _logger.LogWarning($"⚠️ [SendGroupedImages] Bot no encontrado: {convo.BotId}");
                    throw new HubException($"Bot {convo.BotId} no encontrado.");
                }

                foreach (var file in multipleFiles)
                {
                    try
                    {
                        _logger.LogInformation($"📄 [SendGroupedImages] Procesando archivo: {file.FileName}");

                        // ✅ The fileUrl comes from previous sendChatFile calls
                        // Format: /api/files/chat/{id}
                        // Extract the ID from the fileUrl
                        if (string.IsNullOrWhiteSpace(file.FileUrl))
                        {
                            _logger.LogWarning($"❌ [SendGroupedImages] Archivo sin fileUrl: {file.FileName}");
                            continue;
                        }

                        // Parse fileId from fileUrl (e.g., "/api/files/chat/312" → 312)
                        int fileId = 0;
                        if (file.FileUrl.Contains("/api/files/chat/"))
                        {
                            var idStr = file.FileUrl.Replace("/api/files/chat/", "").Split('/')[0];
                            if (int.TryParse(idStr, out var parsedId))
                            {
                                fileId = parsedId;
                            }
                        }

                        if (fileId <= 0)
                        {
                            _logger.LogWarning($"❌ [SendGroupedImages] No se pudo extraer ID del fileUrl: {file.FileUrl}");
                            continue;
                        }

                        // ✅ Just create a message linked to the existing file
                        var fileMessage = new Message
                        {
                            BotId = convo.BotId,
                            UserId = userId,
                            PublicUserId = convo.PublicUserId,  // ✅ Incluir PublicUserId para consistencia
                            ConversationId = conversationId,
                            Sender = "user",
                            MessageText = $"📎 {file.FileName}",
                            FileId = fileId,  // ✅ Link to existing ChatUploadedFile
                            Source = "widget",
                            CreatedAt = DateTime.UtcNow,
                            Status = "sent"
                        };

                        messagesToAdd.Add(fileMessage);

                        fileDtos.Add(new
                        {
                            fileName = file.FileName,
                            fileType = file.FileType,
                            fileUrl = file.FileUrl  // ✅ Use the fileUrl from sendChatFile
                        });

                        _logger.LogInformation($"✅ [SendGroupedImages] Mensaje creado para archivo: {file.FileName} (FileId: {fileId})");
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError($"❌ [SendGroupedImages] Error procesando archivo {file.FileName}: {fileEx.Message}");
                        // Continue with next file instead of throwing
                    }
                }

                // ✅ Save all messages at once
                if (messagesToAdd.Count > 0)
                {
                    _context.Messages.AddRange(messagesToAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ [SendGroupedImages] {messagesToAdd.Count} mensajes guardados en DB");
                }

                _logger.LogInformation($"📸 [SendGroupedImages] Completado. Enviando {fileDtos.Count} archivos al grupo.");

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    files = fileDtos,
                    text = "",
                    timestamp = DateTime.UtcNow
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "user",
                    text = "Se enviaron múltiples imágenes.",
                    files = fileDtos,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"✅ [SendGroupedImages] Notificaciones enviadas exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SendGroupedImages] Error general al enviar imágenes agrupadas. Detalles: {ExceptionDetails}", ex.ToString());
                throw new HubException($"Ocurrió un error al enviar las imágenes: {ex.Message}");
            }
        }
        // ✅ Renombrado para claridad y corregido para notificar solo a los OTROS clientes.
        [AllowAnonymous] // ✅ Widget puede notificar que está escribiendo
        public async Task Typing(int conversationId, string userId)
        {
            if (conversationId > 0 && !string.IsNullOrWhiteSpace(userId))
            {
                // Notifica a los otros miembros del grupo de la conversación
                await Clients.OthersInGroup(conversationId.ToString()).SendAsync("ReceiveTyping", conversationId, userId);
                // Notifica al grupo de administradores
                await Clients.Group("admin").SendAsync("ReceiveTyping", conversationId, userId);
            }
        }

        // ✅ Renombrado para claridad y corregido para notificar solo a los OTROS clientes.
        [AllowAnonymous] // ✅ Widget puede notificar que dejó de escribir
        public async Task StopTyping(int conversationId, string userId)
        {
            if (conversationId > 0 && !string.IsNullOrWhiteSpace(userId))
            {
                // Notifica a los otros miembros del grupo que un usuario dejó de escribir.
                await Clients.OthersInGroup(conversationId.ToString()).SendAsync("ReceiveStopTyping", conversationId, userId);
                // Notifica al grupo de administradores
                await Clients.Group("admin").SendAsync("ReceiveStopTyping", conversationId, userId);
            }
        }

        [AllowAnonymous] // ✅ Widget puede enviar archivos
        public async Task SendFile(int conversationId, object payload)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

            try
            {
                var json = JsonSerializer.Serialize(payload);
                var fileObj = JsonSerializer.Deserialize<ChatFileDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Si el payload puede ser null o no traer contenido, devolver error al caller
                if (fileObj == null)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Archivo inválido.",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }

                // Nota histórica: originalmente el cliente convertía el archivo a base64 y lo enviaba
                // por SignalR en la propiedad `fileContent`. Esto causaba overhead de memoria y red.
                //
                // Optimización: soportamos ahora que el cliente suba por multipart/form-data
                // a /api/ChatUploadedFiles y nos pase aquí la `fileUrl` resultante. En ese caso
                // saltamos la decodificación base64 y usamos directamente la URL.

                // Prueba rápida (frontend):
                // - Subir archivo desde widget/dashboard: el cliente llamará POST /api/ChatUploadedFiles (multipart).
                // - El servidor devuelve filePath (p.e. /uploads/chat/abcd.pdf).
                // - El cliente invoca connection.invoke("SendFile", conversationId, { fileUrl: filePath, fileName, fileType, userId });
                // - El Hub reutiliza el registro si existe y envía ReceiveMessage con files[] a los clientes del grupo.

                string? finalPath = null;

                if (!string.IsNullOrWhiteSpace(fileObj.FileUrl))
                {
                    // Caso nuevo: ya subido por multipart y recibimos directamente la ruta
                    finalPath = fileObj.FileUrl;
                }
                else
                {
                    // Flujo legacy: procesar base64 y guardar archivo
                    if (string.IsNullOrWhiteSpace(fileObj.FileContent))
                    {
                        await Clients.Caller.SendAsync("ReceiveMessage", new
                        {
                            conversationId,
                            from = "bot",
                            text = "⚠️ Archivo inválido.",
                            timestamp = DateTime.UtcNow
                        });
                        return;
                    }

                    var base64Data = fileObj.FileContent.Contains(",")
                        ? fileObj.FileContent.Split(',')[1]
                        : fileObj.FileContent;

                    byte[] fileBytes;
                    try
                    {
                        fileBytes = Convert.FromBase64String(base64Data);

                        if (fileBytes.Length > 10 * 1024 * 1024) // 10 MB
                            throw new InvalidOperationException("El archivo es demasiado grande.");
                    }
                    catch (Exception ex)
                    {
                        await Clients.Caller.SendAsync("ReceiveMessage", new
                        {
                            conversationId,
                            from = "bot",
                            text = $"⚠️ Error al procesar el archivo: {ex.Message}",
                            timestamp = DateTime.UtcNow
                        });
                        return;
                    }

                    finalPath = await _chatFileService.SaveBase64FileAsync(base64Data, fileObj.FileName ?? string.Empty);
                }

                // Evitar duplicados: si el archivo ya fue creado por el endpoint multipart, reutilizarlo
                if (string.IsNullOrWhiteSpace(finalPath))
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        from = "bot",
                        text = "⚠️ Ruta de archivo inválida.",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }

                // Normalizar valores para evitar nulls al persistir
                fileObj.FileName = fileObj.FileName ?? "archivo";
                fileObj.FileType = fileObj.FileType ?? "application/octet-stream";

                // ✅ BUSCAR archivo existente: primero por ID en URL /api/files/chat/{id}, luego por FilePath
                ChatUploadedFile? existing = null;
                
                if (finalPath.Contains("/api/files/chat/"))
                {
                    // El cliente envió la URL de API — extraer el ID para buscar directamente
                    var idStr = finalPath.Replace("/api/files/chat/", "").Split('/')[0].Split('?')[0];
                    if (int.TryParse(idStr, out var parsedFileId) && parsedFileId > 0)
                    {
                        existing = await _context.ChatUploadedFiles.FindAsync(parsedFileId);
                        if (existing != null)
                        {
                            _logger.LogInformation($"✅ [SendFile] ChatUploadedFile encontrado por ID de API URL: {parsedFileId}");
                        }
                    }
                }
                
                // Fallback: buscar por FilePath exacto
                if (existing == null)
                {
                    existing = await _context.ChatUploadedFiles
                        .FirstOrDefaultAsync(f => f.FilePath == finalPath && f.ConversationId == conversationId);
                }

                ChatUploadedFile dbFile;
                if (existing != null)
                {
                    dbFile = existing;
                }
                else
                {
                    // Normarize and truncate file type to fit DB column (MaxLength 50)
                    var fileTypeRaw = fileObj.FileType ?? "application/octet-stream";
                    var fileTypeSafe = fileTypeRaw.Length > 50 ? fileTypeRaw.Substring(0, 50) : fileTypeRaw;

                    dbFile = new ChatUploadedFile
                    {
                        ConversationId = conversationId,
                        UserId = fileObj.UserId,
                        FileName = fileObj.FileName ?? "archivo",
                        FileType = fileTypeSafe,
                        FilePath = finalPath
                    };

                    _context.ChatUploadedFiles.Add(dbFile);
                    await _context.SaveChangesAsync();
                }

                // ✅ Crear un registro Message vinculado al archivo para que aparezca en el historial
                var existingMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.FileId == dbFile.Id && m.ConversationId == conversationId);
                
                if (existingMessage == null)
                {
                    var convo = await _context.Conversations.FindAsync(conversationId);
                    var fileMessage = new Message
                    {
                        BotId = convo?.BotId,
                        UserId = fileObj.UserId,
                        PublicUserId = convo?.PublicUserId,
                        ConversationId = conversationId,
                        Sender = "user",
                        MessageText = $"📎 {dbFile.FileName}",
                        FileId = dbFile.Id,
                        Source = "widget",
                        CreatedAt = DateTime.UtcNow,
                        Status = "sent"
                    };
                    _context.Messages.Add(fileMessage);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ [SendFile] Mensaje creado para archivo: {dbFile.FileName} (FileId: {dbFile.Id}, MessageId: {fileMessage.Id})");
                }

                // ✅ Siempre usar URL de API segura, no la ruta física
                var safeFileUrl = $"/api/files/chat/{dbFile.Id}";

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    files = new[] { new
                    {
                        fileName = dbFile.FileName,
                        fileType = dbFile.FileType,
                        fileUrl = safeFileUrl
                    }},
                    timestamp = DateTime.UtcNow
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "user",
                    text = "📎 Se envió un archivo.",
                    alias = $"Sesión {conversationId}",
                    lastMessage = "📎 Se envió un archivo.",
                    timestamp = DateTime.UtcNow,
                    files = new[] { new
                    {
                        fileName = dbFile.FileName,
                        fileType = dbFile.FileType,
                        fileUrl = safeFileUrl
                    }}
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en SendFile: {Message}", ex.Message);
                // Responder al caller con un mensaje legible
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = $"⚠️ Error al procesar el archivo: {ex.Message}",
                    timestamp = DateTime.UtcNow,
                });
                return;
            }

        }

        /// <summary>
        /// ✅ NUEVA ARQUITECTURA: Actualizar estado de un mensaje después del upload.
        /// 
        /// Flujo profesional (como WhatsApp):
        /// 1. Cliente crea messageId único (UUID)
        /// 2. Envía mensaje con estado "pending"
        /// 3. Upload inicia en background con progress tracking
        /// 4. Al completar, cliente invoca UpdateMessage con fileUrl
        /// 5. Hub notifica al grupo que el mensaje fue actualizado
        /// 
        /// Beneficios:
        /// - Preview local inmediato (blob URL)
        /// - Progress bar visible durante upload
        /// - Retry automático si falla
        /// - Sin duplicación de datos (base64 solo local)
        /// </summary>
        [AllowAnonymous] // ✅ Widget puede actualizar estado de mensajes/archivos
        public async Task UpdateMessage(UpdateFileUploadStatusDto dto)
        {
            try
            {
                _logger.LogInformation("🔄 [UpdateMessage] Actualizando mensaje {messageId} en conversación {convId}. Status: {status}",
                    dto.MessageId, dto.ConversationId, dto.Status);

                // Validar que la conversación exista
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == dto.ConversationId);

                if (conversation == null)
                {
                    _logger.LogError("❌ [UpdateMessage] Conversación {convId} no encontrada", dto.ConversationId);
                    await Clients.Caller.SendAsync("Error", "Conversación no encontrada");
                    return;
                }

                // Si el upload fue exitoso, actualizar el mensaje con los detalles del archivo
                if (dto.Status == "sent" && dto.FileId.HasValue)
                {
                    var fileRecord = await _context.ChatUploadedFiles
                        .FirstOrDefaultAsync(f => f.Id == dto.FileId.Value);

                    if (fileRecord != null)
                    {
                        _logger.LogInformation("✅ [UpdateMessage] Archivo {fileId} vinculado. Enviando actualización al grupo", dto.FileId.Value);
                    }
                }

                // Notificar a TODOS en la conversación que el mensaje fue actualizado
                // (incluyendo al sender para suavizar transición blob URL → CDN URL)
                await Clients.Group(dto.ConversationId.ToString())
                    .SendAsync("MessageUpdated", new
                    {
                        messageId = dto.MessageId,
                        conversationId = dto.ConversationId,
                        fileUrl = dto.FileUrl,
                        fileId = dto.FileId,
                        status = dto.Status,
                        uploadProgress = dto.UploadProgress,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation("✅ [UpdateMessage] Notificación enviada al grupo {convId}", dto.ConversationId);

                // También notificar al panel admin
                await Clients.Group("admin").SendAsync("MessageUpdated", new
                {
                    conversationId = dto.ConversationId,
                    messageId = dto.MessageId,
                    fileUrl = dto.FileUrl,
                    status = dto.Status,
                    uploadProgress = dto.UploadProgress
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [UpdateMessage] Error actualizando mensaje {messageId}: {msg}", dto.MessageId, ex.Message);
                await Clients.Caller.SendAsync("Error", $"Error al actualizar mensaje: {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 MÓVIL MULTI-DISPOSITIVO: Notificar al backend que una sesión móvil se unió
        /// Se llama desde el frontend móvil después de conectarse a SignalR
        /// </summary>
        [HubMethodName("NotifyMobileJoined")]
        public async Task NotifyMobileJoined(int conversationId, string deviceType)
        {
            try
            {
                _logger.LogInformation("📱 [Hub] Móvil se unió a conversación {convId} - Tipo: {device}", conversationId, deviceType);

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning("⚠️ [Hub] Conversación {convId} no encontrada", conversationId);
                    return;
                }

                // Actualizar estado en BD
                conversation.ActiveMobileSession = true;
                conversation.MobileDeviceType = deviceType ?? "mobile";
                conversation.MobileJoinedAt = DateTime.UtcNow;
                conversation.LastActiveAt = DateTime.UtcNow;
                conversation.Blocked = true; // Bloquear web
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ [Hub] Estado actualizado - Conversación {convId} bloqueada", conversationId);

                // Notificar a TODOS en el grupo (incluyendo web)
                await Clients.Group(conversationId.ToString())
                    .SendAsync("MobileSessionStarted", new
                    {
                        conversationId,
                        deviceType,
                        joinedAt = conversation.MobileJoinedAt
                    });

                _logger.LogInformation("📢 [Hub] Evento 'MobileSessionStarted' enviado al grupo {convId}", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [NotifyMobileJoined] Error: {msg}", ex.Message);
            }
        }

        /// <summary>
        /// 🆕 MÓVIL MULTI-DISPOSITIVO: Notificar al backend que una sesión móvil se cerró
        /// Se llama desde el frontend móvil cuando cierra o expira (30s inactividad)
        /// </summary>
        [HubMethodName("NotifyMobileClosed")]
        public async Task NotifyMobileClosed(int conversationId)
        {
            try
            {
                _logger.LogInformation("📱 [Hub] Móvil cerró conversación {convId}", conversationId);

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning("⚠️ [Hub] Conversación {convId} no encontrada", conversationId);
                    return;
                }

                // Marcar como cerrada
                conversation.ActiveMobileSession = false;
                conversation.Status = "closed";
                conversation.ClosedAt = DateTime.UtcNow;
                conversation.Blocked = false; // Desbloquear web
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ [Hub] Sesión móvil cerrada - Conversación {convId} desbloqueada", conversationId);

                // Notificar a TODOS en el grupo (incluyendo web)
                await Clients.Group(conversationId.ToString())
                    .SendAsync("MobileSessionEnded", new
                    {
                        conversationId,
                        reason = "mobile-closed",
                        closedAt = conversation.ClosedAt
                    });

                _logger.LogInformation("📢 [Hub] Evento 'MobileSessionEnded' enviado al grupo {convId}", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [NotifyMobileClosed] Error: {msg}", ex.Message);
            }
        }

        /// <summary>
        /// 📧 EMAIL NOTIFICATIONS: Verificar si hay admins online y enviar email si están offline
        /// </summary>
        private async Task NotifyAdminIfOfflineAsync(int conversationId, string messageText)
        {
            try
            {
                var conversation = await _context.Conversations
                    .Include(c => c.AssignedUser)
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null) return;

                // Verificar si hay admins conectados en los últimos 5 minutos
                var recentlyActiveAdminIds = await _context.ActivityLogs
                    .Where(log => log.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
                    .Select(log => log.UserId)
                    .Distinct()
                    .ToListAsync();

                bool hasOnlineAdmin = recentlyActiveAdminIds.Any();

                if (!hasOnlineAdmin)
                {
                    // No hay admins online - incrementar contador y enviar email
                    conversation.UnreadAdminMessages++;
                    await _context.SaveChangesAsync();

                    // Buscar un admin para notificar (el asignado o cualquier admin)
                    string? adminEmail = null;
                    if (conversation.AssignedUserId.HasValue && conversation.AssignedUser != null)
                    {
                        adminEmail = conversation.AssignedUser.Email;
                    }
                    else
                    {
                        // Buscar cualquier admin con role "Admin"
                        var admin = await _userManager.GetUsersInRoleAsync("Admin");
                        adminEmail = admin.FirstOrDefault()?.Email;
                    }

                    if (!string.IsNullOrEmpty(adminEmail))
                    {
                        await _emailService.SendAdminNotificationAsync(
                            adminEmail,
                            conversationId.ToString(),
                            messageText,
                            conversation.UnreadAdminMessages
                        );

                        _logger.LogInformation(
                            $"📧 Email de notificación enviado a {adminEmail} - Sesión {conversationId}, " +
                            $"{conversation.UnreadAdminMessages} mensajes sin leer");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ No se encontró admin para notificar - Sesión {conversationId}");
                    }
                }
                else
                {
                    _logger.LogDebug($"👤 Hay admins online, no enviar email - Sesión {conversationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al notificar admin offline - Sesión {conversationId}");
                // No relanzar para evitar que falle el flujo del mensaje
            }
        }

    }

}