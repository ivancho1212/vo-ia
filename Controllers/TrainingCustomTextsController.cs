using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;

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
        public async Task<ActionResult<IEnumerable<TrainingCustomTextResponseDto>>> GetAll()
        {
            var texts = await _context.TrainingCustomTexts
                .Select(t => new TrainingCustomTextResponseDto
                {
                    Id = t.Id,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Content = t.Content,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToListAsync();

            return Ok(texts);
        }

        /// <summary>
        /// Obtiene los textos personalizados por sesión de entrenamiento.
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<TrainingCustomTextResponseDto>>> GetBySession(int sessionId)
        {
            var texts = await _context.TrainingCustomTexts
                .Where(t => t.TemplateTrainingSessionId == sessionId)
                .Select(t => new TrainingCustomTextResponseDto
                {
                    Id = t.Id,
                    BotTemplateId = t.BotTemplateId,
                    TemplateTrainingSessionId = t.TemplateTrainingSessionId,
                    UserId = t.UserId,
                    Content = t.Content,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToListAsync();

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
                BotTemplateId = dto.BotTemplateId,
                TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                UserId = dto.UserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TrainingCustomTexts.Add(text);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = text.Id }, new TrainingCustomTextResponseDto
            {
                Id = text.Id,
                BotTemplateId = text.BotTemplateId,
                TemplateTrainingSessionId = text.TemplateTrainingSessionId,
                UserId = text.UserId,
                Content = text.Content,
                CreatedAt = text.CreatedAt,
                UpdatedAt = text.UpdatedAt
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
