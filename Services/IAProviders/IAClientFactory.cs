using System;
using Voia.Api.Models.AiModelConfigs;
using Voia.Api.Services.IAProviders;

namespace Voia.Api.Services.IAProviders
{
    public class IAClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public IAClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAApiClient Create(AiModelConfig config)
        {
            return config.IaProvider.Name.ToLower() switch
            {
                "openai" => (IAApiClient)_serviceProvider.GetService(typeof(OpenAIClient)),
                "gemini" => (IAApiClient)_serviceProvider.GetService(typeof(GeminiClient)),
                "google" => (IAApiClient)_serviceProvider.GetService(typeof(GeminiClient)), // 👈 esta línea
                "deepseek" => (IAApiClient)_serviceProvider.GetService(typeof(DeepSeekClient)),
                _ => throw new NotSupportedException("IA provider not supported.")
            };
        }

    }
}
