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

            // Llamada normal
            var response = await _aiProviderService.GetBotResponseAsync(
                request.BotId,
                request.UserId,
                request.Question,
                request.CapturedFields
            );

            Console.WriteLine("📤 Respuesta de IA:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response));

            return Ok(response);
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
