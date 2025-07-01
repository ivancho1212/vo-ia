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
            // Emitir el mensaje del usuario
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId = conversationId,
                from = "user",
                text = request.Question
            });

            // Emitir estado de escritura
            await Clients.Group(conversationId).SendAsync("Typing", new { from = "bot" });

            // Simular respuesta IA
            await Task.Delay(2000);

            var aiResponse = await _aiProviderService.GetBotResponseAsync(
                request.BotId,
                request.UserId,
                request.Question
            );

            var botAnswer = string.IsNullOrWhiteSpace(aiResponse)
                ? "Lo siento, no pude generar una respuesta en este momento."
                : aiResponse;

            // Guardar conversación
            var conversation = new Conversation
            {
                BotId = request.BotId,
                UserId = request.UserId,
                Title = request.Question,
                UserMessage = request.Question,
                BotResponse = botAnswer,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Emitir respuesta IA
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId = conversationId,
                from = "bot",
                text = botAnswer
            });

            // Notificar al grupo admin
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
            // Emitir el mensaje del admin al grupo
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                conversationId = conversationId,
                from = "admin",
                text = text
            });

            // (Opcional) Guardar el mensaje del admin en la base de datos si lo deseas
        }
    }
}
