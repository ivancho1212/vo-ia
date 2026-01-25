using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Voia.Api.Services.IAProviders;
using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Services.IAProviders
{
    public class DeepSeekClient : IAApiClient
    {
    private readonly string _apiKey;
    private readonly string _endpoint;

    public DeepSeekClient(string endpoint, string apiKey)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
    }

    public async Task<string> SendMessageAsync(string prompt, AiModelConfig config)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var requestBody = new
        {
            model = config.ModelName,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"{_endpoint}/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
    }
}
