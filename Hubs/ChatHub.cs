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

namespace Voia.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ApplicationDbContext _context;
        private readonly IChatFileService _chatFileService;
        private static readonly Dictionary<int, bool> PausedConversations = new();
        private readonly ILogger<ChatHub> _logger;
        private readonly TokenCounterService _tokenCounter;
        private const int TypingDelayMs = 2000;


        public ChatHub(
            IAiProviderService aiProviderService,
            ApplicationDbContext context,
            IChatFileService chatFileService,
            ILogger<ChatHub> logger,
            TokenCounterService tokenCounter)  // <-- agregado
        {
            _aiProviderService = aiProviderService;
            _context = context;
            _chatFileService = chatFileService;
            _logger = logger;
            _tokenCounter = tokenCounter;  // <-- asignado
        }


        public async Task JoinRoom(int conversationId)
        {
            if (conversationId <= 0)
            {
                _logger.LogWarning("⚠️ conversationId es inválido.");
                throw new HubException("El ID de conversación debe ser un número positivo.");
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
                _logger.LogInformation("✅ Usuario unido al grupo: {ConversationId}", conversationId);
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
            // Usamos ToListAsync para no bloquear el hilo
            var conversaciones = await _context.Conversations
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    id = c.Id,
                    // ✅ SOLUCIÓN: Enviamos el UserId completo. Es más seguro.
                    alias = $"Usuario {c.UserId}",
                    lastMessage = c.UserMessage,
                    updatedAt = c.UpdatedAt,
                    status = c.Status,
                    blocked = false // Este campo debe venir de la base de datos si existe
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("InitialConversations", conversaciones);
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
                    _logger.LogInformation("🔄 Conversación ya existente.");
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

                _logger.LogInformation("✅ Conversación creada con ID: {ConversationId}", newConversation.Id);
                return newConversation.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en InitializeContext: {ex.Message}");
                throw new HubException("No se pudo inicializar la conversación.");
            }
        }

        // ✅ Método para pausar o activar la IA desde el admin
        public Task SetIAPaused(int conversationId, bool paused)
        {
            PausedConversations[conversationId] = paused;
            Console.WriteLine($"🔁 IA {(paused ? "pausada" : "activada")} para conversación {conversationId}");
            return Task.CompletedTask;
        }
        public async Task SendMessage(int conversationId, AskBotRequestDto request)
        {
            // Verificar que el usuario existe
            var userExists = _context.Users.Any(u => u.Id == request.UserId);

            string? repliedText = null;
            if (request.ReplyToMessageId.HasValue)
            {
                repliedText = _context.Messages
                    .Where(m => m.Id == request.ReplyToMessageId.Value)
                    .Select(m => m.MessageText)
                    .FirstOrDefault();
            }

            if (!userExists)
            {
                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Error: usuario no válido. No se puede procesar el mensaje."
                });
                return;
            }

            // Emitir mensaje del usuario
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "user",
                text = request.Question,
                timestamp = DateTime.UtcNow,
                replyToMessageId = request.ReplyToMessageId,
                replyToText = repliedText
            });

            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "user",
                text = request.Question,
                timestamp = DateTime.UtcNow,
                alias = $"Usuario {request.UserId}",
                lastMessage = request.Question,
                replyToMessageId = request.ReplyToMessageId,
                replyToText = repliedText
            });

            // Buscar o crear la conversación
            var conversation = _context.Conversations
                .Where(c => c.UserId == request.UserId && c.BotId == request.BotId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

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
                await _context.SaveChangesAsync(); // Guardar para obtener el Id
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

            // Contar tokens del usuario y registrar
            if (_tokenCounter != null && !string.IsNullOrWhiteSpace(request?.Question))
            {
                int userTokens = _tokenCounter.CountTokens(request.Question);
                _context.TokenUsageLogs.Add(new TokenUsageLog
                {
                    UserId = request.UserId,
                    BotId = request.BotId,
                    TokensUsed = userTokens,
                    UsageDate = DateTime.UtcNow
                });
            }

            // Actualizar conversación
            conversation.UserMessage = request.Question;
            conversation.LastMessage = request.Question;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Verificar si la IA está pausada
            if (PausedConversations.TryGetValue(conversationId, out var paused) && paused)
            {
                _logger.LogWarning("⏸️ IA pausada. No se responde con IA para conversación {ConversationId}", conversationId);
                return;
            }

            await Clients.Group(conversationId.ToString()).SendAsync("Typing", new { from = "bot" });
            await Task.Delay(TypingDelayMs);

            string botAnswer;

            try
            {
                var aiResponse = await _aiProviderService.GetBotResponseAsync(
                    request.BotId,
                    request.UserId,
                    request.Question
                );

                botAnswer = string.IsNullOrWhiteSpace(aiResponse)
                    ? "Lo siento, no pude generar una respuesta en este momento."
                    : aiResponse;
            }
            catch (NotSupportedException)
            {
                botAnswer = "🤖 Este bot aún no está conectado a un proveedor de IA. Pronto estará disponible.";
            }
            catch (Exception ex)
            {
                botAnswer = "⚠️ Error al procesar el mensaje. Inténtalo más tarde.";
                _logger.LogError(ex, "❌ Error en IA.");
            }

            // Guardar mensaje del bot
            var botMessage = new Message
            {
                BotId = request.BotId,
                UserId = request.UserId,
                ConversationId = conversation.Id,
                Sender = "bot",
                MessageText = botAnswer ?? string.Empty,
                Source = "widget",
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(botMessage);

            // Contar tokens de la respuesta del bot y registrar
            if (_tokenCounter != null && !string.IsNullOrWhiteSpace(botAnswer))
            {
                int botTokens = _tokenCounter.CountTokens(botAnswer);
                _context.TokenUsageLogs.Add(new TokenUsageLog
                {
                    UserId = request.UserId,
                    BotId = request.BotId,
                    TokensUsed = botTokens,
                    UsageDate = DateTime.UtcNow
                });
            }

            // Actualizar respuesta del bot en conversación
            conversation.BotResponse = botAnswer;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "bot",
                text = botAnswer,
                timestamp = DateTime.UtcNow
            });
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
                alias = "Administrador",
                lastMessage = text,
                replyToMessageId = replyToMessageId,
                replyToText = repliedText
            });
        }

        [HubMethodName("SendGroupedImages")]
        public async Task SendGroupedImages(int conversationId, int userId, List<ChatFileDto> multipleFiles)
        {
            try
            {
                _logger.LogInformation("📸 Recibiendo grupo de imágenes.");
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

                    var fileMessage = new Message
                    {
                        BotId = 1, // 🔧 Usa un valor válido
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
                    alias = $"Usuario {userId}",
                    text = "Se enviaron múltiples imágenes.",
                    images = fileDtos,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar imágenes agrupadas: {Message}", ex.Message);
                throw new HubException("Ocurrió un error al enviar las imágenes.");
            }
        }


        public async Task Typing(int conversationId, string sender)
        {
            if (conversationId > 0 && !string.IsNullOrWhiteSpace(sender))
            {
                await Clients.Group(conversationId.ToString()).SendAsync("Typing", sender);
                await Clients.Group("admin").SendAsync("Typing", conversationId.ToString(), sender);
            }
        }

        public async Task SendFile(int conversationId, object payload)
        {
            _logger.LogInformation("📥 Se llamó a SendFile");

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

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "user",
                file = new
                {
                    fileName = dbFile.FileName,
                    fileType = dbFile.FileType,
                    fileUrl = dbFile.FilePath
                },
                timestamp = DateTime.UtcNow
            });

            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "user",
                text = "📎 Se envió un archivo.",
                alias = $"Usuario {fileObj.UserId}",
                lastMessage = "📎 Se envió un archivo.",
                timestamp = DateTime.UtcNow,
                file = new
                {
                    fileName = dbFile.FileName,
                    fileType = dbFile.FileType,
                    fileUrl = dbFile.FilePath
                }
            });
        }

    }

}