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
        public async Task TriggerDocumentProcessingAsync()
        {
            await PostAsync("/process-documents/");
        }

        // 🚀 Procesa URLs pendientes
        public async Task TriggerUrlProcessingAsync()
        {
            await PostAsync("/process-urls/");
        }

        // 🚀 Procesa textos planos pendientes
        public async Task TriggerCustomTextProcessingAsync()
        {
            await PostAsync("/process-custom-texts/");
        }

        // 🔗 Método genérico para llamadas POST
        private async Task PostAsync(string endpoint)
        {
            try
            {
                var fullUrl = new Uri(new Uri(_baseUrl), endpoint);

                Console.WriteLine($"[FASTAPI] ⏩ Llamando a {fullUrl}");

                var response = await _httpClient.PostAsync(fullUrl, null);

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
