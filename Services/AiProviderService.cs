using Voia.Api.Data;
using Voia.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Voia.Api.Services.IAProviders;
using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Services
{
   public class AiProviderService : IAiProviderService
{
    private readonly ApplicationDbContext _context;
    private readonly IAClientFactory _clientFactory;

    public AiProviderService(ApplicationDbContext context, IAClientFactory clientFactory)
    {
        _context = context;
        _clientFactory = clientFactory;
    }

    public async Task<string> GetBotResponseAsync(int botId, int userId, string question)
    {
        var bot = await _context.Bots.Include(b => b.AiModelConfig).ThenInclude(m => m.IaProvider)
                                     .FirstOrDefaultAsync(b => b.Id == botId);
        if (bot == null)
            return "Bot no encontrado";

        var config = bot.AiModelConfig;
        if (config == null || config.IaProvider == null)
            return "Configuración IA no encontrada";

        var client = _clientFactory.Create(config);

        return await client.SendMessageAsync(question, config);
    }
}

}
