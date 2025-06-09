using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Services.IAProviders
{
    public interface IAApiClient
    {
        Task<string> SendMessageAsync(string prompt, AiModelConfig config);
    }
}
