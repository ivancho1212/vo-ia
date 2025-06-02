using Microsoft.AspNetCore.SignalR;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.BotConversation;
using System.Threading.Tasks;

namespace Voia.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IAiProviderService _aiProviderService;

        public ChatHub(IAiProviderService aiProviderService)
        {
            _aiProviderService = aiProviderService;
        }

        public async Task SendMessage(AskBotRequestDto request)
        {
            // Lógica para invocar al proveedor correcto (OpenAI, Gemini, etc.)
            var aiResponse = await _aiProviderService.GetBotResponseAsync(request.BotId, request.UserId, request.Question);

            // Preparar la respuesta
            var response = new BotResponseDto
            {
                Question = request.Question,
                Answer = aiResponse
            };

            // Enviar solo al cliente que preguntó
            await Clients.Caller.SendAsync("ReceiveMessage", response);
        }
    }
}
