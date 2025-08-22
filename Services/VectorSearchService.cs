using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Voia.Api.Services
{
    public class VectorSearchService
    {
        private readonly HttpClient _httpClient;

        public VectorSearchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<object>> SearchVectorsAsync(int botId, string query = "", int limit = 5)
        {
            // URL del endpoint de Python
            var url = $"http://localhost:8000/search_vectors?bot_id={botId}&query={Uri.EscapeDataString(query)}&limit={limit}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            // Deserializamos a lista de objetos
            var result = JsonSerializer.Deserialize<List<object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new List<object>();
        }
    }
}
