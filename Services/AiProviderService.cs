using Voia.Api.Data;
using Voia.Api.Services.Interfaces;

namespace Voia.Api.Services
{
    public class AiProviderService : IAiProviderService
    {
        private readonly ApplicationDbContext _context;

        public AiProviderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetBotResponseAsync(int botId, int userId, string question)
        {
            // Aquí simulas la respuesta del bot
            await Task.Delay(300); // Simula tiempo de respuesta de API
            return $"🤖 Bot {botId} dice: Hola, recibí tu pregunta: \"{question}\"";
        }
    }
}
