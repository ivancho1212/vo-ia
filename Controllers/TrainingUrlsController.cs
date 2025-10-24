
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;
using Voia.Api.Helpers;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingUrlsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainingUrlsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las URLs de entrenamiento.
        /// </summary>
        [HttpGet]
        [HasPermission("CanViewTrainingUrls")]
        public async Task<ActionResult<IEnumerable<TrainingUrlResponseDto>>> GetAll()
        {
            var urls = await _context.TrainingUrls
                .Select(t => new TrainingUrlResponseDto
                {
                    Id = t.Id,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Url = t.Url,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToListAsync();

            return Ok(urls);
        }
        /// <summary>
        /// Obtiene URLs por ID de bot.
        /// </summary>
        [HttpGet("by-bot/{botId}")]
        [HasPermission("CanViewTrainingUrls")]
        public async Task<ActionResult<IEnumerable<TrainingUrlResponseDto>>> GetByBot(int botId)
        {
            var urls = await _context.TrainingUrls
                        .Where(x => x.BotId == botId)
                        .Select(x => new TrainingUrlResponseDto
                        {
                            Id = x.Id,
                            BotId = x.BotId,
                            BotTemplateId = x.BotTemplateId,
                            TemplateTrainingSessionId = x.TemplateTrainingSessionId,
                            UserId = x.UserId,
                            Url = x.Url,
                            Status = x.Status,
                            CreatedAt = x.CreatedAt,
                            UpdatedAt = x.UpdatedAt,
                            QdrantId = x.QdrantId,
                            ContentHash = x.ContentHash,
                            Indexed = x.Indexed,
                            ExtractedText = x.ExtractedText
                        }).ToListAsync();
            return Ok(urls);
        }

        /// <summary>
        /// Obtiene URLs por sesión de entrenamiento.
        /// </summary>
        [HttpGet("session/{sessionId}")]
        [HasPermission("CanViewTrainingUrls")]
        public async Task<ActionResult<IEnumerable<TrainingUrlResponseDto>>> GetBySession(int sessionId)
        {
            var urls = await _context.TrainingUrls
                .Where(t => t.TemplateTrainingSessionId == sessionId)
                .Select(t => new TrainingUrlResponseDto
                {
                    Id = t.Id,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Url = t.Url,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToListAsync();

            return Ok(urls);
        }

        [HttpGet("by-template/{templateId}")]
        [HasPermission("CanViewTrainingUrls")]
        public async Task<ActionResult<IEnumerable<TrainingUrlResponseDto>>> GetByTemplate(int templateId)
        {
            var urls = await _context.TrainingUrls
                        .Where(x => x.BotTemplateId == templateId)
                        .Select(x => new TrainingUrlResponseDto
                        {
                            Id = x.Id,
                            BotTemplateId = x.BotTemplateId,
                            TemplateTrainingSessionId = x.TemplateTrainingSessionId,
                            UserId = x.UserId,
                            Url = x.Url,
                            Status = x.Status,
                            CreatedAt = x.CreatedAt,
                            UpdatedAt = x.UpdatedAt,
                            QdrantId = x.QdrantId,
                            ContentHash = x.ContentHash,
                            Indexed = x.Indexed,
                            ExtractedText = x.ExtractedText
                        })
                        .ToListAsync();

            return Ok(urls);
        }


        /// <summary>
        /// Crea una nueva URL de entrenamiento.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TrainingUrlResponseDto>> Create(TrainingUrlCreateDto dto)
        {
            try
            {
                // Validar que el templateTrainingSessionId (si viene) exista y pertenezca al mismo bot_template_id
                if (dto.TemplateTrainingSessionId.HasValue)
                {
                    var session = await _context.TemplateTrainingSessions
                        .FirstOrDefaultAsync(s => s.Id == dto.TemplateTrainingSessionId.Value && s.BotTemplateId == dto.BotTemplateId);
                    if (session == null)
                    {
                        return BadRequest(new { message = "El ID de sesión de entrenamiento no es válido para la plantilla seleccionada." });
                    }
                }

                // Normalizar la URL
                var normalizedUrl = dto.Url.TrimEnd('/').ToLowerInvariant();
                var contentHash = HashHelper.ComputeStringHash(normalizedUrl);

                Console.WriteLine($"Debug - URL: {dto.Url}");
                Console.WriteLine($"Debug - Normalized URL: {normalizedUrl}");
                Console.WriteLine($"Debug - Content Hash: {contentHash}");
                Console.WriteLine($"Debug - Bot ID: {dto.BotId}");

                var existing = await _context.TrainingUrls
                    .FirstOrDefaultAsync(u => (u.ContentHash == contentHash || u.Url.ToLower() == normalizedUrl) && u.BotId == dto.BotId);

                if (existing != null)
                {
                    Console.WriteLine($"Debug - Found existing URL with ID: {existing.Id}, URL: {existing.Url}");
                    return Conflict(new
                    {
                        message = "⚠️ Esta URL ya fue registrada anteriormente para este bot.",
                        existingId = existing.Id,
                        existingUrl = existing.Url
                    });
                }

                var url = new TrainingUrl
                {
                    BotId = dto.BotId,
                    BotTemplateId = dto.BotTemplateId,
                    TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                    UserId = dto.UserId,
                    Url = dto.Url,
                    ContentHash = contentHash,
                    Status = "pending",
                    Indexed = 0
                };

                _context.TrainingUrls.Add(url);
                await _context.SaveChangesAsync();

                // Nota: No marcamos 'data_capture' aquí.
                // El marcado de la fase `data_capture` debe ocurrir cuando la URL haya sido procesada e indexada
                // por el servicio de ingestión/vectorización (p. ej. el endpoint en localhost:8000). Marcarla ahora
                // causa que la interfaz considere la fase completada por una simple adición, cuando en realidad el
                // trabajo asíncrono de extracción/indexado puede fallar o estar pendiente.

                // Llamar al servicio de vectorización
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync($"http://localhost:8000/process_urls?bot_id={dto.BotId}");

                        if (!response.IsSuccessStatusCode)
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"❌ Error al llamar al servicio de vectorización de URLs: {error}");

                            // No consideramos esto un error fatal, la URL se procesará más tarde
                            Console.WriteLine("ℹ️ La URL se procesará en un intento posterior");
                        }
                        else
                        {
                            Console.WriteLine($"✅ URL enviada a vectorización para el bot {dto.BotId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al contactar el servicio de vectorización: {ex.Message}");
                    // No consideramos esto un error fatal
                    Console.WriteLine("ℹ️ La URL se procesará en un intento posterior");
                }

                return CreatedAtAction(nameof(GetAll), new { id = url.Id }, new TrainingUrlResponseDto
                {
                    Id = url.Id,
                    BotTemplateId = url.BotTemplateId,
                    TemplateTrainingSessionId = url.TemplateTrainingSessionId,
                    UserId = url.UserId,
                    Url = url.Url,
                    Status = url.Status,
                    CreatedAt = url.CreatedAt,
                    UpdatedAt = url.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // Optional: mark data_capture when a URL is created (non-blocking)
        // Note: kept outside try/catch above to not alter behavior, but executed after creation
        // (no need to await here since controller already saved url; we do a fire-and-forget style call is avoided in serverside)



        /// <summary>
        /// Elimina una URL de entrenamiento.
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteTrainingUrls")]
        public async Task<IActionResult> Delete(int id)
        {
            var url = await _context.TrainingUrls.FindAsync(id);
            if (url == null) return NotFound();

            _context.TrainingUrls.Remove(url);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
