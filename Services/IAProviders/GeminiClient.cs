using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Voia.Api.Services.IAProviders;
using Voia.Api.Models.AiModelConfigs;

public class GeminiClient : IAApiClient
{
    private readonly string _apiKey;
    private readonly string _endpoint;

    public GeminiClient(string endpoint, string apiKey)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
    }

    public async Task<string> SendMessageAsync(string prompt, AiModelConfig config)
    {
        using var httpClient = new HttpClient();

        var requestUrl = $"{_endpoint}/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUrl, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        // Ajusta aquí cómo extraer la respuesta según el JSON real que devuelve Gemini
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetString();
    }

    public async Task<bool> TestConnectionAsync()
    {
        // Método opcional para testear conexión
        return true;
    }
}
