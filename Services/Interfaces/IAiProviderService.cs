using System.Collections.Generic;
using System.Threading.Tasks;
using Voia.Api.Models.AiModelConfigs;  // 👈 importante para usar DataField

namespace Voia.Api.Services.Interfaces
{
    public interface IAiProviderService
    {
        Task<string> GetBotResponseAsync(
            int botId, 
            int userId, 
            string question, 
            List<DataField> capturedFields = null
        );
    }
}
