using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Bots;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotPhasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotPhasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/botphases/bot/{botId}
        [HttpGet("bot/{botId}")]
        public async Task<IActionResult> GetByBot(int botId)
        {
            var rows = await _context.BotPhases
                .Where(p => p.BotId == botId)
                .ToListAsync();

            var result = rows.ToDictionary(r => r.Phase, r => new { completed = r.CompletedAt != null, completedAt = r.CompletedAt, meta = r.Meta });

            return Ok(new { phases = result });
        }

        // POST: api/botphases
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UpsertBotPhaseRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Phase)) return BadRequest(new { message = "phase is required" });
            var existing = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == req.BotId && p.Phase == req.Phase);
            if (existing != null)
            {
                existing.CompletedAt = req.Completed ? DateTime.UtcNow : (DateTime?)null;
                existing.Meta = req.Meta;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new BotPhase
                {
                    BotId = req.BotId,
                    Phase = req.Phase,
                    CompletedAt = req.Completed ? DateTime.UtcNow : (DateTime?)null,
                    Meta = req.Meta,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.BotPhases.Add(existing);
            }

            await _context.SaveChangesAsync();

            return Ok(new { phase = existing.Phase, completed = existing.CompletedAt != null, completedAt = existing.CompletedAt });
        }
    }

    public class UpsertBotPhaseRequest
    {
        public int BotId { get; set; }
        public string Phase { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public string? Meta { get; set; }
    }
}
