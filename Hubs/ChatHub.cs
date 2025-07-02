using Microsoft.AspNetCore.SignalR;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.BotConversation;
using Voia.Api.Models.Conversations;
using Voia.Api.Data;
using System.Threading.Tasks;
using System;
using System.Linq;

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

        public async Task SendMessage(string conversationId, AskBotRequestDto request)
        {
            Console.WriteLine($"➡️ Recibido mensaje para BotId: {request.BotId}, UserId: {request.UserId}");

            // Verificar si el usuario existe
            var userExists = _context.Users.Any(u => u.Id == request.UserId);
            if (!userExists)
            {
                Console.WriteLine($"❌ El usuario con ID {request.UserId} no existe. Cancelando.");
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
                {
                    conversationId = conversationId,
                    from = "bot",
                    text = "⚠️ Error: usuario no válido. No se puede procesar el mensaje."
                });
                return;
            }

            // Emitir mensaje del usuario
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId = conversationId,
                from = "user",
                text = request.Question
            });

            // Emitir "escribiendo"
            await Clients.Group(conversationId).SendAsync("Typing", new { from = "bot" });

            await Task.Delay(2000); // Simulación de procesamiento

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

            // Guardar la conversación
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

            // Respuesta del bot
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId = conversationId,
                from = "bot",
                text = botAnswer
            });

            // Notificar a admin
            await Clients.Group("admin").SendAsync("NewConversationOrMessage", new
            {
                conversationId = conversationId,
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
                conversationId = conversationId,
                from = "admin",
                text = text
            });
        }

        public async Task LeaveRoom(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
