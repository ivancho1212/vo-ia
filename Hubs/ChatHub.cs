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

namespace Voia.Api.Hubs
{
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


        public ChatHub(
            IAiProviderService aiProviderService,
            ApplicationDbContext context,
            IChatFileService chatFileService,
            ILogger<ChatHub> logger,
            TokenCounterService tokenCounter,
            BotDataCaptureService dataCaptureService,
            HttpClient httpClient,
            PromptBuilderService promptBuilderService // 👈 agregado
        )
        {
            _aiProviderService = aiProviderService;
            _context = context;
            _chatFileService = chatFileService;
            _logger = logger;
            _tokenCounter = tokenCounter;
            _dataCaptureService = dataCaptureService;
            _httpClient = httpClient;
            _promptBuilderService = promptBuilderService; // 👈 asignado
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
                    string finalContent = c.LastEvent.RawContent;
                    if (c.LastEvent.Type == "text" && !string.IsNullOrEmpty(finalContent) && finalContent.Trim().StartsWith("{"))
                    {
                        try
                        {
                            using (var doc = System.Text.Json.JsonDocument.Parse(finalContent))
                            {
                                if (doc.RootElement.TryGetProperty("UserQuestion", out var userQuestion))
                                {
                                    finalContent = userQuestion.GetString();
                                }
                                else if (doc.RootElement.TryGetProperty("Content", out var content))
                                {
                                    finalContent = content.GetString();
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Not a valid JSON, so we'll just use the original RawContent
                        }
                    }

                    lastMessageString = c.LastEvent.Type switch
                    {
                        "text" => finalContent,
                        "image" => "📷 Imagen",
                        "file" => $"📎 Archivo: {finalContent}",
                        _ => finalContent
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
                    UserId = userId,
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
            string botAnswer = "Lo siento, no pude generar una respuesta en este momento."; // Declaración e inicialización

            // Validación de usuario
            if (!_context.Users.Any(u => u.Id == request.UserId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Error: usuario no válido.",
                    status = "error",
                    tempId = request.TempId
                });
                return;
            }

            // 🔴 SOLUCIÓN: Asegurar que el cliente esté en el grupo de la conversación.
            // Esto es idempotente y soluciona el problema de estado transitorio del Hub.
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

            // Obtener mensaje al que se responde
            string? repliedText = null;
            if (request.ReplyToMessageId.HasValue)
            {
                repliedText = await _context.Messages
                    .Where(m => m.Id == request.ReplyToMessageId.Value)
                    .Select(m => m.MessageText)
                    .FirstOrDefaultAsync();
            }

            // Buscar o crear conversación
            var conversation = await _context.Conversations
                .Where(c => c.UserId == request.UserId && c.BotId == request.BotId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    BotId = request.BotId,
                    UserId = request.UserId,
                    Title = "Primera interacción",
                    CreatedAt = DateTime.UtcNow,
                    Status = "activa"
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
                // Notificar al grupo admin sobre la nueva conversación
                await Clients.Group("admin").SendAsync("NewConversation", new
                {
                    id = conversation.Id,
                    alias = $"Sesión {conversation.Id}", // O la lógica de alias que tengas
                    lastMessage = request.Question,
                    updatedAt = conversation.UpdatedAt,
                    status = conversation.Status,
                    blocked = conversation.Blocked, // Asumiendo que Blocked es una propiedad
                    isWithAI = conversation.IsWithAI // Added IsWithAI
                });
            }

            // Guardar mensaje del usuario
            var userMessage = new Message
            {
                BotId = request.BotId,
                UserId = request.UserId,
                ConversationId = conversation.Id,
                Sender = "user",
                MessageText = request.Question ?? string.Empty,
                Source = "widget",
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = request.ReplyToMessageId
            };
            _context.Messages.Add(userMessage);

            // Contar tokens de usuario
            if (_tokenCounter != null && !string.IsNullOrWhiteSpace(request.Question))
            {
                _context.TokenUsageLogs.Add(new TokenUsageLog
                {
                    UserId = request.UserId,
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
                    request.UserId,
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

            // Si el servicio de captura generó un mensaje de confirmación, lo usamos como respuesta del bot.
            if (!string.IsNullOrEmpty(captureResult.ConfirmationPrompt))
            {
                botAnswer = await _aiProviderService.GetBotResponseAsync(
                    request.BotId,
                    request.UserId,
                    captureResult.ConfirmationPrompt,
                    currentCapturedFields
                );
                goto SendBotResponse; // Saltamos directamente a la sección de envío de respuesta
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

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
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

            // IA pausada
            // Retrieve the conversation again to get the latest IsWithAI status from the database
            // Revisar si la IA está activa ANTES de mostrar typing
            var conversationFromDb = await _context.Conversations.FindAsync(conversationId);
            if (conversationFromDb != null && !conversationFromDb.IsWithAI)
            {
                _logger.LogInformation($"IA pausada para la conversación {conversationId}. No se generará respuesta.");
                return; // No se envía typing ni se procesa IA
            }

            // Solo si la IA está activa, enviamos typing. Usamos "ReceiveTyping" que es el evento que el cliente escucha.
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveTyping", conversationId, "bot");
            await Task.Delay(TypingDelayMs);

            try
            {
                // 1️⃣ Obtener campos capturados desde la DB
                var capturedFields = await _context.BotDataCaptureFields
                    .Where(f => f.BotId == request.BotId)
                    .Select(f => new DataField
                    {
                        FieldName = f.FieldName,
                        // 🔹 CORRECCIÓN: Usamos el conversationId como identificador de sesión único para aislar los datos por visitante.
                        Value = _context.BotDataSubmissions.Where(s =>
                                s.BotId == request.BotId && s.CaptureFieldId == f.Id &&
                                s.SubmissionSessionId == conversationId.ToString())
                            .OrderByDescending(s => s.SubmittedAt) // Tomamos el más reciente para esta sesión
                            .Select(s => s.SubmissionValue)
                            .FirstOrDefault()
                    })
            .ToListAsync();

                string finalPrompt = await _promptBuilderService.BuildPromptFromBotContextAsync(
                    request.BotId,
                    request.UserId,
                    request.Question ?? "",
                    currentCapturedFields // ✅ FIX: Usamos la lista que ya fue actualizada por el servicio de captura.
                );

                // 3️⃣ Llamar al AI provider con el prompt final que contiene todo el contexto.
                botAnswer = await _aiProviderService.GetBotResponseAsync(
                    request.BotId,
                    request.UserId,
                    finalPrompt, // Pasamos el JSON completo
                    currentCapturedFields // ✅ FIX: Pasamos la lista actualizada.
                ) ?? "Lo siento, no pude generar una respuesta en este momento.";
            }
            catch (NotSupportedException)

            {
                botAnswer = "🤖 Este bot aún no está conectado a un proveedor de IA.";
            }
            catch (Exception ex)
            {
                botAnswer = "⚠️ Error al procesar el mensaje. Inténtalo más tarde.";
                _logger.LogError(ex, "❌ Error en IA.");
            }

        SendBotResponse:

            // 4️⃣ Deserializar JSON (si es mock)
            string displayText = botAnswer;
            try
            {
                var jsonResponse = JsonDocument.Parse(botAnswer);
                if (jsonResponse.RootElement.TryGetProperty("Answer", out var answerElement))
                {
                    displayText = answerElement.GetString() ?? botAnswer;
                }
            }
            catch (JsonException)
            {
                // No es JSON, usamos tal cual
            }

            // --- 🔹 INICIO: Procesar y guardar CapturedFields desde la respuesta de la IA ---
            try
            {
                var jsonResponseForCapture = JsonDocument.Parse(botAnswer);
                if (jsonResponseForCapture.RootElement.TryGetProperty("CapturedFields", out var capturedFieldsElement) && capturedFieldsElement.ValueKind == JsonValueKind.Array)
                {
                    var fieldsFromAi = capturedFieldsElement.Deserialize<List<DataField>>();
                    if (fieldsFromAi != null && fieldsFromAi.Any(f => !string.IsNullOrEmpty(f.Value)))
                    {

                        var fieldNamesFromAi = fieldsFromAi.Select(f => f.FieldName).ToList();
                        var fieldDefinitions = await _context.BotDataCaptureFields
                            .Where(f => f.BotId == request.BotId && fieldNamesFromAi.Contains(f.FieldName))
                            .ToDictionaryAsync(f => f.FieldName, f => f.Id, StringComparer.OrdinalIgnoreCase);

                        var batchSubmissions = new List<BotDataSubmissionCreateDto>();
                        // 👇 FILTRADO: Procesar solo los campos que tienen un valor no nulo/vacío.
                        var validFieldsFromAi = fieldsFromAi.Where(f => !string.IsNullOrEmpty(f.Value));

                        foreach (var field in validFieldsFromAi)
                        {
                            if (fieldDefinitions.TryGetValue(field.FieldName, out var captureFieldId))
                            {
                                batchSubmissions.Add(new BotDataSubmissionCreateDto
                                {
                                    BotId = request.BotId,
                                    CaptureFieldId = captureFieldId,
                                    SubmissionValue = field.Value,
                                    UserId = request.UserId,
                                    SubmissionSessionId = conversationId.ToString()
                                });
                            }

                        }

                        if (batchSubmissions.Any())
                        {

                            // Usar HttpClient para enviar al endpoint batch
                            await _httpClient.PostAsJsonAsync("api/botdatasubmissions/batch", batchSubmissions);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al procesar y guardar los CapturedFields de la respuesta de la IA.");
            }
            // --- 🔹 FIN: Procesar y guardar CapturedFields ---

            // Guardar mensaje del bot
            var botMessage = new Message
            {
                BotId = request.BotId,
                UserId = request.UserId,
                ConversationId = conversation.Id,
                Sender = "bot",
                MessageText = displayText, // ✅ Guardar el texto limpio
                Source = "widget",
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(botMessage);

            // Contar tokens de la respuesta
            if (_tokenCounter != null && !string.IsNullOrWhiteSpace(botAnswer))
            {
                _context.TokenUsageLogs.Add(new TokenUsageLog
                {
                    UserId = request.UserId,
                    BotId = request.BotId,
                    TokensUsed = _tokenCounter.CountTokens(botAnswer),
                    UsageDate = DateTime.UtcNow
                });
            }

            // Actualizar conversación con respuesta
            conversation.BotResponse = botAnswer;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            // Console.WriteLine($"[ChatHub] Sending ReceiveMessage for single file. ConversationId: {conversationId}"); // Eliminado

            // Enviar respuesta al grupo
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "bot", // ✅ Cambiado a "bot" para consistencia con el resto del frontend
                text = displayText, // ✅ Usamos el texto extraído
                timestamp = botMessage.CreatedAt,
                id = botMessage.Id
            });
            await StopTyping(conversationId, "bot");
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

            // ✅ Emitir hacia el grupo de conversación
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "admin",
                text,
                timestamp = DateTime.UtcNow,
                replyToMessageId = replyToMessageId,
                replyToText = repliedText
            });

            // ✅ Emitir hacia el grupo admin
            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "admin",
                text,
                timestamp = DateTime.UtcNow,
                alias = $"Sesión {conversationId}",
                lastMessage = text,
                replyToMessageId = replyToMessageId,
                replyToText = repliedText
            });

            // Detener el indicador de "escribiendo" para el admin
            await StopTyping(conversationId, "admin");
        }

        [HubMethodName("SendGroupedImages")]
        public async Task SendGroupedImages(int conversationId, int userId, List<ChatFileDto> multipleFiles)
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
                        UserId = userId,
                        FileName = file.FileName,
                        FileType = file.FileType,
                        FilePath = finalPath
                    };

                    _context.ChatUploadedFiles.Add(dbFile);
                    await _context.SaveChangesAsync();

                    // Retrieve the conversation to get the correct BotId
                    var convo = await _context.Conversations.FindAsync(conversationId);
                    if (convo == null)
                    {
                        continue; // Skip saving this message if conversation not found
                    }

                    // Explicitly load the Bot to ensure it's in the DbContext's cache
                    // This might help if EF Core is somehow not seeing the Bot with ID 4
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

                    // Console.WriteLine($"[ChatHub] Saving file message with BotId: {fileMessage.BotId}"); // Eliminado
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

            var json = JsonSerializer.Serialize(payload);
            var fileObj = JsonSerializer.Deserialize<ChatFileDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fileObj == null || string.IsNullOrWhiteSpace(fileObj.FileContent))
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

            // Procesar base64 y guardar archivo
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


            var filePath = await _chatFileService.SaveBase64FileAsync(base64Data, fileObj.FileName);

            var dbFile = new ChatUploadedFile
            {
                ConversationId = conversationId,
                UserId = fileObj.UserId,
                FileName = fileObj.FileName,
                FileType = fileObj.FileType,
                FilePath = filePath
            };


            _context.ChatUploadedFiles.Add(dbFile);
            await _context.SaveChangesAsync();
            // Console.WriteLine($"[ChatHub] Sending ReceiveMessage for single file. ConversationId: {conversationId}"); // Eliminado
            // System.Text.Json.JsonSerializer.Serialize(...) // Eliminado

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

    }

}