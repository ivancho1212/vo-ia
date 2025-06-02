using System.Threading.Tasks;

namespace Voia.Api.Services.Interfaces
{
    public interface IAiProviderService
    {
        Task<string> GetBotResponseAsync(int botId, int userId, string question);
    }
}
