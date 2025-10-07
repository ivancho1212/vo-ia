using Microsoft.AspNetCore.Mvc;
using Voia.Api.Services;
using Voia.Api.Models.SearchRequest;
using System;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VectorSearchController : ControllerBase
    {
        private readonly VectorSearchService _vectorSearchService;

        public VectorSearchController(VectorSearchService vectorSearchService)
        {
            _vectorSearchService = vectorSearchService;
        }

    [HttpPost("search")]
    [HasPermission("CanSearchVectors")]
    public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
        {
            if (request == null)
                return BadRequest("La solicitud no puede ser nula.");

            if (request.BotId <= 0 || string.IsNullOrWhiteSpace(request.Query))
                return BadRequest("BotId y Query son requeridos.");

            try
            {
                var vectorResults = await _vectorSearchService.SearchVectorsAsync(
                    request.BotId,
                    request.Query,
                    request.Limit > 0 ? request.Limit : 5
                );

                if (vectorResults == null)
                    return NotFound("No se encontraron resultados.");

                return Ok(vectorResults); // El frontend recibe { results: [...] }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en VectorSearchController: {ex}");
                return StatusCode(500, "Ocurrió un error interno al procesar la búsqueda vectorial.");
            }
        }
    }
}
