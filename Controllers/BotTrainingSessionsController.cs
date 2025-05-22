using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.BotTrainingSession;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotTrainingSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotTrainingSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotTrainingSessionResponseDto>>> GetAll()
        {
            var sessions = await _context.BotTrainingSessions
                .Select(s => new BotTrainingSessionResponseDto
                {
                    Id = s.Id,
                    BotId = s.BotId,
                    SessionName = s.SessionName,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToListAsync();

            return Ok(sessions);
        }

        [HttpPost]
        public async Task<ActionResult<BotTrainingSessionResponseDto>> Create(BotTrainingSessionCreateDto dto)
        {
            var session = new BotTrainingSession
            {
                BotId = dto.BotId,
                SessionName = dto.SessionName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotTrainingSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = new BotTrainingSessionResponseDto
            {
                Id = session.Id,
                BotId = session.BotId,
                SessionName = session.SessionName,
                Description = session.Description,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };

            return CreatedAtAction(nameof(GetAll), new { id = session.Id }, result);
        }
    }
}
