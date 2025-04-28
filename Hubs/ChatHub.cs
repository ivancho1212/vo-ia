using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Voia.Api.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            var response = await GetOpenAIResponse(message);
            await Clients.All.SendAsync("ReceiveMessage", user, message, response);
        }

        private Task<string> GetOpenAIResponse(string message)
        {
            return Task.FromResult("Respuesta de OpenAI a: " + message);
        }
    }
}
