using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;

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
        /// Obtiene URLs por sesión de entrenamiento.
        /// </summary>
        [HttpGet("session/{sessionId}")]
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

        /// <summary>
        /// Crea una nueva URL de entrenamiento.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TrainingUrlResponseDto>> Create(TrainingUrlCreateDto dto)
        {
            try
            {
                var url = new TrainingUrl
                {
                    BotTemplateId = dto.BotTemplateId,
                    TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                    UserId = dto.UserId,
                    Url = dto.Url,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TrainingUrls.Add(url);
                await _context.SaveChangesAsync();

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


        /// <summary>
        /// Elimina una URL de entrenamiento.
        /// </summary>
        [HttpDelete("{id}")]
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
