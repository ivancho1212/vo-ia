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

namespace Voia.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ApplicationDbContext _context;

        public ChatHub(IAiProviderService aiProviderService, ApplicationDbContext context)
        {
            _aiProviderService = aiProviderService;
            _context = context;
        }

        public async Task JoinRoom(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveRoom(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
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
        public async Task InitializeContext(string conversationId, object data)
        {
            try
            {
                // Extraer botId y userId del objeto dinámico
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int botId = root.GetProperty("botId").GetInt32();
                int userId = root.GetProperty("userId").GetInt32();

                // Verifica si ya existe una conversación
                var existing = _context.Conversations
                    .FirstOrDefault(c => c.UserId == userId && c.BotId == botId);

                if (existing != null)
                {
                    Console.WriteLine("🔄 Conversación ya existente.");
                    return;
                }

                // Crea la conversación vacía (sin mensajes aún)
                var newConversation = new Conversation
                {
                    BotId = botId,
                    UserId = userId,
                    Title = "Nueva conversación",
                    UserMessage = null,
                    BotResponse = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(newConversation);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Contexto inicial preparado para usuario {userId} con bot {botId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en InitializeContext: {ex.Message}");
            }
        }

        public async Task SendMessage(string conversationId, AskBotRequestDto request)
        {
            var userExists = _context.Users.Any(u => u.Id == request.UserId);
            if (!userExists)
            {
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "bot",
                    text = "⚠️ Error: usuario no válido. No se puede procesar el mensaje."
                });
                return;
            }

            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "user",
                text = request.Question,
                timestamp = DateTime.UtcNow
            });

            await Clients.Group(conversationId).SendAsync("Typing", new { from = "bot" });

            await Task.Delay(2000); // Simula procesamiento IA

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

            var conversation = new Conversation
            {
                BotId = request.BotId,
                UserId = request.UserId,
                Title = request.Question,
                UserMessage = request.Question,
                BotResponse = botAnswer,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar conversación: {ex.Message}");
                botAnswer = "⚠️ Error al guardar la conversación.";
            }

            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "bot",
                text = botAnswer,
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
        }

        public async Task AdminMessage(string conversationId, string text)
        {
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId,
                from = "admin",
                text,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task SendFile(string conversationId, object payload)
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
                // Imágenes
                "image/jpeg", "image/png", "image/webp", "image/gif",

                // Documentos
                "application/pdf",
                "application/msword",                             // .doc
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
                "application/vnd.ms-excel",                       // .xls
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
                "application/vnd.ms-powerpoint",                 // .ppt
                "application/vnd.openxmlformats-officedocument.presentationml.presentation", // .pptx
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

                    const int maxSize = 5 * 1024 * 1024; // 5 MB
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

            // Enviar como mensaje agrupado
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
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
                lastMessage = $"📎 Se enviaron {validFiles.Count} archivo(s)."
            });
        }
    }
}
