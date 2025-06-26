using System.Net.Http;
using System.Threading.Tasks;

namespace Voia.Api.Services
{
    public class FastApiService
    {
        private readonly HttpClient _httpClient;

        public FastApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task TriggerDocumentProcessingAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("http://localhost:8000/process-documents/", null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("[FASTAPI] Procesamiento exitoso: " + content);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("[FASTAPI] Error en procesamiento: " + error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FASTAPI ERROR] No se pudo contactar con el servicio de procesamiento: " + ex.Message);
            }
        }
    }
}
