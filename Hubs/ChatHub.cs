using Microsoft.AspNetCore.SignalR;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.BotConversation;
using Voia.Api.Models.Conversations;
using Voia.Api.Data;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Voia.Api.Models.Messages;


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
                timestamp = DateTime.UtcNow
            });

            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "user",
                text = request.Question,
                timestamp = DateTime.UtcNow,
                alias = $"Usuario {request.UserId}",
                lastMessage = request.Question
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
                CreatedAt = DateTime.UtcNow
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
            var wrapper = JsonSerializer.Deserialize<FilePayloadWrapper>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (wrapper == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ No se recibió archivo.",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            var files = new List<FilePayload>();

            if (wrapper.MultipleFiles != null && wrapper.MultipleFiles.Any())
                files.AddRange(wrapper.MultipleFiles);
            else if (wrapper.File != null)
                files.Add(wrapper.File);

            if (!files.Any())
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Archivo inválido o vacío.",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            var allowedMimeTypes = new[]
            {
                "image/jpeg", "image/png", "image/webp", "image/gif",
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            };

            var validFiles = new List<FilePayload>();

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FileContent) || !allowedMimeTypes.Contains(file.FileType))
                    continue;

                try
                {
                    var base64Data = file.FileContent.Contains(",")
                        ? file.FileContent.Split(',')[1]
                        : file.FileContent;

                    var bytes = Convert.FromBase64String(base64Data);

                    const int maxSize = 5 * 1024 * 1024;
                    if (bytes.Length <= maxSize)
                        validFiles.Add(file);
                }
                catch
                {
                    continue;
                }
            }

            if (!validFiles.Any())
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Ninguno de los archivos es válido.",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            Console.WriteLine($"✅ Archivos válidos recibidos: {validFiles.Count}");

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "user",
                multipleFiles = validFiles,
                timestamp = DateTime.UtcNow
            });

            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId,
                from = "user",
                text = $"📎 Se enviaron {validFiles.Count} archivo(s).",
                timestamp = DateTime.UtcNow,
                alias = "Usuario",
                lastMessage = $"📎 Se enviaron {validFiles.Count} archivo(s).",
                multipleFiles = validFiles
            });
        }
    }
}
