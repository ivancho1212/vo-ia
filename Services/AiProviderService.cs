using Voia.Api.Data;
using Voia.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Threading.Tasks;
using Voia.Api.Services.IAProviders;
using Voia.Api.Models.AiModelConfigs;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services
{
    public class AiProviderService : IAiProviderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAClientFactory _clientFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PromptBuilderService> _promptBuilderLogger;

        public AiProviderService(ApplicationDbContext context, IAClientFactory clientFactory, IHttpClientFactory httpClientFactory, ILogger<PromptBuilderService> promptBuilderLogger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _httpClientFactory = httpClientFactory;
            _promptBuilderLogger = promptBuilderLogger;
        }

        public async Task<string> GetBotResponseAsync(int botId, int userId, string question, List<DataField> capturedFields = null)
        {
            var bot = await _context.Bots
                .Include(b => b.AiModelConfig)
                .ThenInclude(m => m.IaProvider)
                .FirstOrDefaultAsync(b => b.Id == botId);

            if (bot == null)
                return "Bot no encontrado";

            var config = bot.AiModelConfig;
            if (config == null || config.IaProvider == null)
                return "Configuración IA no encontrada";

            var client = _clientFactory.Create(config);

            // 🔹 El prompt (question) ya viene construido desde el ChatHub.
            // Simplemente lo pasamos al cliente de la IA.
            return await client.SendMessageAsync(question, config);
        }

    }
}
