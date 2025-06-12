using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Voia.Api.Data;
using Voia.Api.Models; // ← Aquí se encuentra BotTemplate y TemplateTrainingSession
using Voia.Api.Models.Dtos;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateTrainingSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TemplateTrainingSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea una nueva sesión de entrenamiento para una plantilla.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TemplateTrainingSession session)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar que exista la plantilla
            var templateExists = await _context.BotTemplates.AnyAsync(t => t.Id == session.BotTemplateId);
            if (!templateExists)
                return NotFound($"No existe plantilla con ID {session.BotTemplateId}");

            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _context.TemplateTrainingSessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(session);
        }
        [HttpPost("with-prompts")]
        public async Task<IActionResult> CreateWithPrompts([FromBody] TemplateTrainingSessionWithPromptsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var templateExists = await _context.BotTemplates.AnyAsync(t => t.Id == dto.BotTemplateId);
            if (!templateExists)
                return NotFound($"No existe plantilla con ID {dto.BotTemplateId}");

            var session = new TemplateTrainingSession
            {
                BotTemplateId = dto.BotTemplateId,
                SessionName = dto.SessionName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TemplateTrainingSessions.Add(session);
            await _context.SaveChangesAsync(); // Guardamos primero para obtener el ID

            var prompts = dto.Prompts.Select(p => new BotCustomPrompt
            {
                Role = p.Role,
                Content = p.Content,
                BotTemplateId = dto.BotTemplateId, // ← ESTA LÍNEA FALTABA
                TemplateTrainingSessionId = session.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();


            _context.BotCustomPrompts.AddRange(prompts);
            await _context.SaveChangesAsync();

            return Ok(new { session.Id });
        }

        /// <summary>
        /// Obtiene todas las sesiones de entrenamiento asociadas a una plantilla.
        /// </summary>
        [HttpGet("by-template/{templateId}")]
        public async Task<IActionResult> GetByTemplateId(int templateId)
        {
            var sessions = await _context.TemplateTrainingSessions
                .Where(s => s.BotTemplateId == templateId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }
    }
}
