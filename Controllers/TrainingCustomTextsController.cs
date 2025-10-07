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
    public class TrainingCustomTextsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainingCustomTextsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los textos personalizados de entrenamiento.
        /// </summary>
    [HttpGet]
    [HasPermission("CanViewTrainingCustomTexts")]
    public async Task<ActionResult<IEnumerable<TrainingCustomTextResponseDto>>> GetAll()
        {
            var texts = await _context.TrainingCustomTexts
                .Select(t => new TrainingCustomTextResponseDto
                {
                    Id = t.Id,
                    BotId = t.BotId,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Content = t.Content,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Indexed = t.Indexed,
                    QdrantId = t.QdrantId,
                    ContentHash = t.ContentHash
                }).ToListAsync();

            return Ok(texts);
        }

        /// <summary>
        /// Obtiene los textos personalizados por sesión de entrenamiento.
        /// </summary>
    [HttpGet("session/{sessionId}")]
    [HasPermission("CanViewTrainingCustomTexts")]
    public async Task<ActionResult<IEnumerable<TrainingCustomTextResponseDto>>> GetBySession(int sessionId)
        {
            var texts = await _context.TrainingCustomTexts
                .Where(t => t.TemplateTrainingSessionId == sessionId)
                .Select(t => new TrainingCustomTextResponseDto
                {
                    Id = t.Id,
                    BotId = t.BotId,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Content = t.Content,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Indexed = t.Indexed,
                    QdrantId = t.QdrantId,
                    ContentHash = t.ContentHash
                }).ToListAsync();

            return Ok(texts);
        }

    [HttpGet("by-template/{templateId}")]
    [HasPermission("CanViewTrainingCustomTexts")]
    public async Task<ActionResult<IEnumerable<TrainingCustomTextResponseDto>>> GetByTemplate(int templateId)
        {
            var texts = await _context.TrainingCustomTexts
                        .Where(x => x.BotTemplateId == templateId)
                        .Select(x => new TrainingCustomTextResponseDto
                        {
                            Id = x.Id,
                            BotId = x.BotId,
                            BotTemplateId = x.BotTemplateId,
                            TemplateTrainingSessionId = x.TemplateTrainingSessionId,
                            UserId = x.UserId,
                            Content = x.Content,
                            Status = x.Status,
                            CreatedAt = x.CreatedAt,
                            UpdatedAt = x.UpdatedAt,
                            QdrantId = x.QdrantId,
                            ContentHash = x.ContentHash,
                            Indexed = x.Indexed
                        })
                        .ToListAsync();

            return Ok(texts);
        }


        /// <summary>
        /// Crea un nuevo texto personalizado de entrenamiento.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TrainingCustomTextResponseDto>> Create(TrainingCustomTextCreateDto dto)
        {
            var text = new TrainingCustomText
            {
                BotId = dto.BotId,
                BotTemplateId = dto.BotTemplateId,
                TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                UserId = dto.UserId,
                Content = dto.Content,
                Status = "pending",
                Indexed = 0
            };

            // Calcular hash del contenido
            text.ContentHash = HashHelper.ComputeStringHash(dto.Content);
            
            // Verificar si ya existe un texto idéntico
            var existing = await _context.TrainingCustomTexts
                .FirstOrDefaultAsync(t => t.ContentHash == text.ContentHash && t.BotTemplateId == dto.BotTemplateId);

            if (existing != null)
            {
                return Conflict(new
                {
                    message = "⚠️ Este texto ya fue registrado anteriormente.",
                    existingId = existing.Id
                });
            }

            _context.TrainingCustomTexts.Add(text);
            await _context.SaveChangesAsync();

            // Llamar al servicio de vectorización
            try 
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync($"http://localhost:8000/process_texts?bot_id={dto.BotId}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error al llamar al servicio de vectorización de textos: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Texto enviado a vectorización para el bot {dto.BotId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al contactar el servicio de vectorización: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetAll), new { id = text.Id }, new TrainingCustomTextResponseDto
            {
                Id = text.Id,
                BotId = text.BotId,
                BotTemplateId = text.BotTemplateId,
                TemplateTrainingSessionId = text.TemplateTrainingSessionId,
                UserId = text.UserId,
                Content = text.Content,
                Status = text.Status,
                CreatedAt = text.CreatedAt,
                UpdatedAt = text.UpdatedAt,
                Indexed = text.Indexed,
                QdrantId = text.QdrantId,
                ContentHash = text.ContentHash
            });
        }

        /// <summary>
        /// Elimina un texto personalizado.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var text = await _context.TrainingCustomTexts.FindAsync(id);
            if (text == null) return NotFound();

            _context.TrainingCustomTexts.Remove(text);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
