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

namespace Voia.Api.Hubs
{
    using Microsoft.AspNetCore.Authorization;

    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ApplicationDbContext _context;
        private readonly IChatFileService _chatFileService;
        private readonly ILogger<ChatHub> _logger;
        private readonly TokenCounterService _tokenCounter;
        private readonly BotDataCaptureService _dataCaptureService;
        private readonly HttpClient _httpClient; // <--- HttpClient inyectado
        private const int TypingDelayMs = 1000; // 1 segundo
        private readonly PromptBuilderService _promptBuilderService;


    // Optional presence service - may be null if Redis not configured
    private readonly PresenceService? _presenceService;
    private readonly IMessageQueue? _messageQueue;

        public ChatHub(
            IAiProviderService aiProviderService,
            ApplicationDbContext context,
            IChatFileService chatFileService,
            ILogger<ChatHub> logger,
            TokenCounterService tokenCounter,
            BotDataCaptureService dataCaptureService,
            HttpClient httpClient,
            PromptBuilderService promptBuilderService,
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
            _presenceService = presenceService;
            _messageQueue = messageQueue;
        }

        public async Task JoinRoom(int conversationId)
        {
            if (conversationId <= 0)
            {
                throw new HubException("El ID de conversación debe ser un número positivo.");
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            }
            catch (Exception ex)
            {
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

        // ✅ Método para pausar o activar la IA desde el admin

        public async Task SendMessage(int conversationId, AskBotRequestDto request)
        {
            try
            {
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
                    _logger.LogError($"❌ [SendMessage] No se pudo determinar el usuario para el mensaje");
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
                    Source = "widget",
                    CreatedAt = DateTime.UtcNow,
                    ReplyToMessageId = request.ReplyToMessageId
                };
                _context.Messages.Add(userMessage);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID del mensaje

                _logger.LogInformation($"✅ [SendMessage] Mensaje de usuario guardado - PublicUserId: {messagePublicUserId}");

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
            var currentCapturedFields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == request.BotId)
                .Select(f => new DataField
                {
                    FieldName = f.FieldName,
                    Value = _context.BotDataSubmissions.Where(s =>
                            s.BotId == request.BotId && s.CaptureFieldId == f.Id &&
                            s.SubmissionSessionId == conversationId.ToString())
                        .OrderByDescending(s => s.SubmittedAt)
                        .Select(s => s.SubmissionValue)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 🔹 PASO 2: Procesar el mensaje para capturar nuevos datos
            var captureResult = await _dataCaptureService.ProcessMessageAsync(
                request.BotId,
                request.UserId,
                conversationId.ToString(), // Usamos el ID de conversación como ID de sesión
                request.Question ?? string.Empty,
                currentCapturedFields // ✅ FIX: Pasamos la lista de campos que obtuvimos
            );

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
                        TempId = request.TempId ?? string.Empty
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

            await StopTyping(conversationId, "user");

            // Enqueue message for background processing (AI) to keep Hub responsive
            try
            {
                if (_messageQueue == null)
                {
                    _logger.LogWarning("Message queue not available, falling back to inline processing for conversation {conv}", conversationId);
                    // As fallback, we keep previous behavior (could be improved) - but to avoid duplication we simply return
                    return;
                }

                var job = new MessageJob
                {
                    ConversationId = conversation.Id,
                    BotId = request.BotId,
                    UserId = request.UserId,
                    MessageId = userMessage.Id,
                    Question = request.Question ?? string.Empty,
                    TempId = request.TempId ?? string.Empty
                };

                // If the conversation has AI paused, do NOT enqueue a job. Admin should reply manually.
                if (!conversation.IsWithAI)
                {
                    _logger.LogInformation("AI is paused for conversation {conv}. Not enqueuing job {msg}", conversationId, userMessage.Id);

                    // Only send the 'paused' system message once per conversation (avoid spamming the widget).
                    try
                    {
                        var pausedTextSnippet = "El asistente está temporalmente pausado";
                        var lastSystem = await _context.Messages
                            .Where(m => m.ConversationId == conversation.Id && m.Sender == "system")
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => new { m.MessageText, m.CreatedAt })
                            .FirstOrDefaultAsync();

                        var shouldSendPaused = true;
                        if (lastSystem != null && !string.IsNullOrEmpty(lastSystem.MessageText) && lastSystem.MessageText.Contains(pausedTextSnippet))
                        {
                            shouldSendPaused = false;
                        }

                        if (shouldSendPaused)
                        {
                            // Persist a system message so it becomes part of the conversation history
                            var sysMsg = new Voia.Api.Models.Messages.Message
                            {
                                BotId = conversation.BotId,
                                UserId = null,
                                PublicUserId = conversation.PublicUserId,
                                ConversationId = conversation.Id,
                                Sender = "system",
                                MessageText = "El asistente está temporalmente pausado. Un agente humano te responderá.",
                                Source = "system",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Messages.Add(sysMsg);
                            await _context.SaveChangesAsync();

                            await Clients.Caller.SendAsync("ReceiveMessage", new
                            {
                                conversationId,
                                from = "system",
                                text = sysMsg.MessageText,
                                timestamp = sysMsg.CreatedAt,
                                id = sysMsg.Id,
                                status = "paused",
                                tempId = request.TempId
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking/sending paused notification for conversation {conv}", conversationId);
                        // Best-effort: do not block the request flow if this check fails
                    }

                    return;
                }

                await _messageQueue.EnqueueAsync(job);

                _logger.LogInformation("Message enqueued for conversation {conv} message {msg}", conversationId, userMessage.Id);

                // Optionally notify the client that the message was queued (status: queued)
                await Clients.Caller.SendAsync("MessageQueued", new { conversationId, messageId = userMessage.Id, tempId = request.TempId });

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue message job for conversation {conv}", conversationId);
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

        public async Task AdminMessage(int conversationId, string text, int? replyToMessageId = null, string? replyToText = null)
        {
            var convo = await _context.Conversations.FindAsync(conversationId);

            string? repliedText = null;

            if (replyToMessageId.HasValue)
            {
                repliedText = _context.Messages
                    .Where(m => m.Id == replyToMessageId.Value)
                    .Select(m => m.MessageText)
                    .FirstOrDefault();
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

    [HubMethodName("SendGroupedImages")]
    public async Task SendGroupedImages(int conversationId, int? userId, List<ChatFileDto> multipleFiles)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            try
            {
                var fileDtos = new List<object>();

                foreach (var file in multipleFiles)
                {
                    string finalPath;

                    if (!string.IsNullOrWhiteSpace(file.FileUrl))
                    {
                        finalPath = file.FileUrl;
                    }
                    else if (!string.IsNullOrWhiteSpace(file.FileContent))
                    {
                        var base64Data = file.FileContent.Contains(",")
                            ? file.FileContent.Split(',')[1]
                            : file.FileContent;

                        finalPath = await _chatFileService.SaveBase64FileAsync(base64Data, file.FileName);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Archivo inválido: sin URL ni contenido base64.");
                        continue;
                    }

                    var dbFile = new ChatUploadedFile
                    {
                        ConversationId = conversationId,
                        PublicUserId = userId,
                        FileName = file.FileName ?? "archivo",
                        FileType = file.FileType ?? "application/octet-stream",
                        FilePath = finalPath
                    };

                    _context.ChatUploadedFiles.Add(dbFile);
                    await _context.SaveChangesAsync();

                    var convo = await _context.Conversations.FindAsync(conversationId);
                    if (convo == null)
                    {
                        continue; // Skip saving this message if conversation not found
                    }


                    var bot = await _context.Bots.FindAsync(convo.BotId);
                    if (bot == null)
                    {
                        continue; // Skip saving this message if bot not found
                    }

                    var fileMessage = new Message
                    {
                        BotId = convo.BotId, // Use the BotId from the conversation
                        UserId = userId,
                        ConversationId = conversationId,
                        Sender = "user",
                        MessageText = $"📎 {dbFile.FileName}",
                        Source = "widget",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Messages.Add(fileMessage);
                    await _context.SaveChangesAsync();

                    fileDtos.Add(new
                    {
                        fileName = dbFile.FileName,
                        fileType = dbFile.FileType,
                        fileUrl = dbFile.FilePath
                    });
                }
                Console.WriteLine($"[ChatHub] Sending ReceiveMessage for grouped images. ConversationId: {conversationId}");

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    images = fileDtos,
                    text = "", // para no enviarlo vacío
                    timestamp = DateTime.UtcNow
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "user",
                    alias = $"Sesión {conversationId}", // Changed from Usuario {userId}
                    text = "Se enviaron múltiples imágenes.",
                    images = fileDtos,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar imágenes agrupadas. Detalles: {ExceptionDetails}", ex.ToString());
                throw new HubException("Ocurrió un error al enviar las imágenes.");
            }
        }


        // ✅ Renombrado para claridad y corregido para notificar solo a los OTROS clientes.
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

                finalPath = await _chatFileService.SaveBase64FileAsync(base64Data, fileObj.FileName);
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

                var existing = await _context.ChatUploadedFiles
                    .FirstOrDefaultAsync(f => f.FilePath == finalPath && f.ConversationId == conversationId);

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

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "user",
                    files = new[] { new
                    {
                        fileName = dbFile.FileName,
                        fileType = dbFile.FileType,
                        fileUrl = dbFile.FilePath
                    }},
                    timestamp = DateTime.UtcNow
                });

                await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
                {
                    conversationId,
                    from = "user",
                    text = "📎 Se envió un archivo.",
                    alias = $"Sesión {conversationId}", // Changed from Usuario {fileObj.UserId}
                    lastMessage = "📎 Se envió un archivo.",
                    timestamp = DateTime.UtcNow,
                    files = new[] { new
                    {
                        fileName = dbFile.FileName,
                        fileType = dbFile.FileType,
                        fileUrl = dbFile.FilePath
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

    }

}