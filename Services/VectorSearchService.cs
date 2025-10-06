using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;

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
            try
            {
                // URL del endpoint de Python
                var url = $"http://localhost:8000/search_vectors?bot_id={botId}&query={Uri.EscapeDataString(query)}&limit={limit}";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Timeout aún más corto
                var response = await _httpClient.GetAsync(url, cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[VectorSearch] Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return new List<object>(); // Retornar lista vacía en caso de error
                }

                var json = await response.Content.ReadAsStringAsync();
                // Deserializamos a lista de objetos
                var result = JsonSerializer.Deserialize<List<object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new List<object>();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"[VectorSearch] Timeout: Servicio de vectores no disponible en puerto 8000");
                return new List<object>(); // Retornar lista vacía por timeout
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[VectorSearch] Connection error: {ex.Message}");
                return new List<object>(); // Retornar lista vacía por error de conexión
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VectorSearch] Exception: {ex.Message}");
                return new List<object>(); // Retornar lista vacía en caso de excepción
            }
        }
    }
}
