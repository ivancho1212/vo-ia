using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.TrainingDataSessions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingDataSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainingDataSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TrainingDataSessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadTrainingDataSessionDto>>> GetAll()
        {
            var sessions = await _context.TrainingDataSessions.ToListAsync();

            var result = sessions.ConvertAll(s => new ReadTrainingDataSessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                BotId = s.BotId,
                DataSummary = s.DataSummary,
                DataType = s.DataType,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            });

            return Ok(result);
        }

        // GET: api/TrainingDataSessions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTrainingDataSessionDto>> GetById(int id)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound();

            return Ok(new ReadTrainingDataSessionDto
            {
                Id = session.Id,
                UserId = session.UserId,
                BotId = session.BotId,
                DataSummary = session.DataSummary,
                DataType = session.DataType,
                Status = session.Status,
                CreatedAt = session.CreatedAt
            });
        }

        // POST: api/TrainingDataSessions
        [HttpPost]
        public async Task<ActionResult<ReadTrainingDataSessionDto>> Create([FromBody] CreateTrainingDataSessionDto dto)
        {
            var session = new TrainingDataSession
            {
                UserId = dto.UserId,
                BotId = dto.BotId,
                DataSummary = dto.DataSummary,
                DataType = dto.DataType,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingDataSessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
        }

        // PUT: api/TrainingDataSessions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainingDataSessionDto dto)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound();

            session.DataSummary = dto.DataSummary ?? session.DataSummary;
            session.DataType = dto.DataType ?? session.DataType;
            session.Status = dto.Status ?? session.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/TrainingDataSessions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.TrainingDataSessions.FindAsync(id);
            if (session == null) return NotFound();

            _context.TrainingDataSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
