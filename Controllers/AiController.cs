using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voia.Api.Services;
using Voia.Api.Services.Interfaces;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // 👈 opcional, si quieres que requiera token como tus otros endpoints
    public class AiController : ControllerBase
    {
        private readonly IAiProviderService _aiProviderService;

        public AiController(IAiProviderService aiProviderService)
        {
            _aiProviderService = aiProviderService;
        }

        [HttpPost("GetResponse")]
        public async Task<IActionResult> GetResponse([FromBody] AiRequestDto request)
        {
            Console.WriteLine("📥 Request recibido:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(request));

            // 🔹 Lógica temporal hasta que actives los proveedores reales
            // Simula la extracción de datos del `request.Question`
            var extractedName = request.Question.Contains("cipress hill") ? "cipress hill" : "Nombre Simulado";
            var extractedAddress = request.Question.Contains("suba") ? "cerca a suba" : null;

            var fakeResponse = new
            {
                BotId = request.BotId,
                UserId = request.UserId,
                Question = request.Question,
                Answer = $"🤖 (Respuesta simulada) El bot {request.BotId} recibió tu pregunta: '{request.Question}'",
                CapturedFields = new List<object>
                {
                    new { FieldName = "Nombre", Value = extractedName },
                    new { FieldName = "Direccion", Value = extractedAddress }
                }
            };

            Console.WriteLine("📤 Respuesta de IA simulada:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(fakeResponse));

            // Simular asincronía (opcional)
            await Task.Delay(100);

            return Ok(fakeResponse);
        }


    }

    // 👇 DTO que mapea lo que manda tu frontend
    public class AiRequestDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; }
        public List<DataField> CapturedFields { get; set; }
    }

}
