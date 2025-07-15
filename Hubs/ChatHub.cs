using Microsoft.AspNetCore.SignalR;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.Conversations;
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



namespace Voia.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ApplicationDbContext _context;

        // ✅ Diccionario para controlar el estado de pausa de la IA
        private static readonly Dictionary<int, bool> PausedConversations = new();

        public ChatHub(IAiProviderService aiProviderService, ApplicationDbContext context)
        {
            _aiProviderService = aiProviderService;
            _context = context;
        }

        public async Task JoinRoom(int conversationId)
        {
            if (conversationId <= 0)
            {
                Console.WriteLine("⚠️ conversationId es inválido.");
                throw new HubException("El ID de conversación debe ser un número positivo.");
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
                Console.WriteLine($"✅ Usuario unido al grupo: {conversationId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en JoinRoom: {ex.Message}");
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
            var conversaciones = _context.Conversations
                .GroupBy(c => c.UserId)
                .Select(g => new
                {
                    id = g.Key,
                    alias = $"Usuario {g.Key.ToString().Substring(0, 4)}",
                    lastMessage = g.OrderByDescending(c => c.CreatedAt).First().UserMessage,
                    updatedAt = g.Max(c => c.CreatedAt),
                    status = "activa",
                    blocked = false
                })
                .ToList();

            await Clients.Caller.SendAsync("InitialConversations", conversaciones);
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
                    Console.WriteLine("🔄 Conversación ya existente.");
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

                Console.WriteLine($"✅ Conversación creada con ID: {newConversation.Id}");
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


            // ✅ Verificar si IA está pausada
            if (PausedConversations.TryGetValue(conversationId, out var paused) && paused)
            {
                Console.WriteLine($"⏸️ IA pausada. No se responde con IA para conversación {conversationId}");
                return;
            }

            await Clients.Group(conversationId.ToString()).SendAsync("Typing", new { from = "bot" });

            await Task.Delay(2000);

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
                Console.WriteLine($"❌ Error en IA: {ex.Message}");
            }

            // ✅ Buscar si ya existe una conversación entre este usuario y este bot
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
                await _context.SaveChangesAsync(); // Necesario para obtener el ID generado
            }

            // ✅ Actualizar último mensaje y fecha de actualización
            conversation.UserMessage = request.Question;
            conversation.BotResponse = botAnswer;
            conversation.LastMessage = request.Question;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ✅ Guardar mensajes en la tabla `messages`
            _context.Messages.AddRange(new[]
            {
            new Message
            {
                BotId = request.BotId,
                UserId = request.UserId,
                ConversationId = conversation.Id,
                Sender = "user",
                MessageText = request.Question,
                Source = "widget",
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = request.ReplyToMessageId
            },
            new Message
            {
                BotId = request.BotId,
                UserId = request.UserId,
                ConversationId = conversation.Id,
                Sender = "bot",
                MessageText = botAnswer,
                Source = "widget",
                CreatedAt = DateTime.UtcNow
            }
        });

            await _context.SaveChangesAsync();


            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "bot",
                text = botAnswer,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task AdminMessage(int conversationId, string text, int? replyToMessageId = null)
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
            Console.WriteLine("📸 Recibiendo grupo de imágenes.");

            var fileDtos = new List<object>();

            foreach (var file in multipleFiles)
            {
                string finalPath;

                // 🔁 Si ya viene con URL, no volver a guardar archivo
                if (!string.IsNullOrWhiteSpace(file.FileUrl))
                {
                    finalPath = file.FileUrl;
                }
                else if (!string.IsNullOrWhiteSpace(file.FileContent))
                {
                    var base64Data = file.FileContent.Contains(",")
                        ? file.FileContent.Split(',')[1]
                        : file.FileContent;

                    byte[] fileBytes = Convert.FromBase64String(base64Data);
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueName = $"{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat", uniqueName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await File.WriteAllBytesAsync(path, fileBytes);

                    finalPath = $"/uploads/chat/{uniqueName}";
                }
                else
                {
                    continue; // 🔴 ni base64 ni URL, se ignora
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

                fileDtos.Add(new
                {
                    fileName = dbFile.FileName,
                    fileType = dbFile.FileType,
                    fileUrl = dbFile.FilePath
                });
            }

            // ✅ Emitir al grupo del usuario
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "user",
                images = fileDtos,
                timestamp = DateTime.UtcNow
            });

            // ✅ Notificar al grupo de admins
            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "user",
                alias = $"Usuario {userId}",
                text = "🖼️ Se enviaron múltiples imágenes.",
                images = fileDtos,
                timestamp = DateTime.UtcNow
            });
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
            Console.WriteLine("📥 Se llamó a SendFile");

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
            }
            catch
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Error al procesar el archivo.",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            var extension = Path.GetExtension(fileObj.FileName);
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
            Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsPath, uniqueFileName);

            await File.WriteAllBytesAsync(fullPath, fileBytes);

            var dbFile = new ChatUploadedFile
            {
                ConversationId = conversationId,
                UserId = fileObj.UserId,
                FileName = fileObj.FileName,
                FileType = fileObj.FileType,
                FilePath = $"/uploads/chat/{uniqueFileName}"
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