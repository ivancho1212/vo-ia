using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Voia.Api.Models.AiModelConfigs;
using Voia.Api.Services.IAProviders;

namespace Voia.Api.Services.IAProviders
{
    public class OpenAIClient : IAApiClient
    {
        private readonly HttpClient _httpClient;

        public OpenAIClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendMessageAsync(string prompt, AiModelConfig config)
        {
            var request = new
            {
                model = config.ModelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = config.Temperature,
                frequency_penalty = config.FrequencyPenalty,
                presence_penalty = config.PresencePenalty
            };

            var requestBody = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.IaProvider.ApiKey);

            var response = await _httpClient.PostAsync(config.IaProvider.ApiEndpoint, requestBody);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
