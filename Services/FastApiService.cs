using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Voia.Api.Services
{
    public class FastApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FastApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["FastApi:BaseUrl"] ?? throw new ArgumentNullException("FastApi:BaseUrl");
        }

        // 🚀 Procesa documentos PDF pendientes
        public async Task TriggerDocumentProcessingAsync(int botId)
        {
            await GetAsync($"/process_documents?bot_id={botId}");
        }

        // 🚀 Procesa URLs pendientes
        public async Task TriggerUrlProcessingAsync(int botId)
        {
            await GetAsync($"/process_urls?bot_id={botId}");
        }

        // 🚀 Procesa textos planos pendientes
        public async Task TriggerCustomTextProcessingAsync(int botId)
        {
            await GetAsync($"/process_texts?bot_id={botId}");
        }

        // 🔗 Método genérico para llamadas GET
        public async Task DeleteVectorCollectionAsync(int botId)
        {
            await DeleteAsync($"/delete_collection?bot_id={botId}");
        }

        private async Task DeleteAsync(string endpoint)
        {
            try
            {
                var fullUrl = new Uri(new Uri(_baseUrl), endpoint);
                Console.WriteLine($"[FASTAPI] ⏩ Llamando a DELETE {fullUrl}");
                var response = await _httpClient.DeleteAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[FASTAPI] ✔️ Eliminación exitosa: {content}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[FASTAPI] ❌ Error en la eliminación: {error}");
                    // No lanzamos excepción para no interrumpir el rollback si la colección ya no existía.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FASTAPI] 🔥 Error crítico en DELETE: {ex.Message}");
                // No relanzar para asegurar que el resto del rollback continúe.
            }
        }

        private async Task GetAsync(string endpoint)
        {
            try
            {
                var fullUrl = new Uri(new Uri(_baseUrl), endpoint);

                Console.WriteLine($"[FASTAPI] ⏩ Llamando a {fullUrl}");

                var response = await _httpClient.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("[FASTAPI] ✔️ Procesamiento exitoso: " + content);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("[FASTAPI] ❌ Error en procesamiento: " + error);
                    throw new Exception($"Error en FastAPI: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FASTAPI] 🔥 Error crítico: " + ex.Message);
                throw;
            }
        }
    }
}
