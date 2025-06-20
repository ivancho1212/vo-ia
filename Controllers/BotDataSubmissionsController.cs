using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotDataSubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotDataSubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/botdatasubmissions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotDataSubmissionResponseDto>>> GetAll()
        {
            var submissions = await _context.BotDataSubmissions
                .Select(s => new BotDataSubmissionResponseDto
                {
                    Id = s.Id,
                    BotId = s.BotId,
                    CaptureFieldId = s.CaptureFieldId,
                    SubmissionValue = s.SubmissionValue,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();

            return Ok(submissions);
        }

        // GET: api/botdatasubmissions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BotDataSubmissionResponseDto>> GetById(int id)
        {
            var submission = await _context.BotDataSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound();

            return new BotDataSubmissionResponseDto
            {
                Id = submission.Id,
                BotId = submission.BotId,
                CaptureFieldId = submission.CaptureFieldId,
                SubmissionValue = submission.SubmissionValue,
                SubmittedAt = submission.SubmittedAt
            };
        }
        // GET: api/botdatasubmissions/by-bot/{botId}
        [HttpGet("by-bot/{botId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubmissionsGroupedByBot(int botId)
        {
            var fields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == botId)
                .ToListAsync();

            var fieldNames = fields.ToDictionary(f => f.Id, f => f.FieldName);

            var submissions = await _context.BotDataSubmissions
                .Where(s => s.BotId == botId)
                .ToListAsync();

            // Agrupar por alguna lógica, por ejemplo, por un usuario que tú determines
            // Si no tienes "user_id", podrías agrupar por combinación de valores (hash) o por campo extra
            var grouped = submissions
                .GroupBy(s => s.CaptureFieldId) // Este es simplificado
                .Select(group => new
                {
                    Field = fieldNames[group.Key],
                    Values = group.Select(g => g.SubmissionValue).ToList()
                });

            return Ok(grouped);
        }

        // POST: api/botdatasubmissions
        [HttpPost]
        public async Task<ActionResult<BotDataSubmissionResponseDto>> Create(BotDataSubmissionCreateDto dto)
        {
            if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.SubmissionSessionId))
            {
                return BadRequest(new { Message = "Debe especificar un userId o un submissionSessionId para asociar el origen del dato." });
            }

            var submission = new BotDataSubmission
            {
                BotId = dto.BotId,
                CaptureFieldId = dto.CaptureFieldId,
                SubmissionValue = dto.SubmissionValue,
                UserId = dto.UserId,
                SubmissionSessionId = dto.SubmissionSessionId,
                SubmittedAt = DateTime.UtcNow
            };

            _context.BotDataSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            var response = new BotDataSubmissionResponseDto
            {
                Id = submission.Id,
                BotId = submission.BotId,
                CaptureFieldId = submission.CaptureFieldId,
                SubmissionValue = submission.SubmissionValue,
                UserId = submission.UserId,
                SubmissionSessionId = submission.SubmissionSessionId,
                SubmittedAt = submission.SubmittedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = submission.Id }, response);
        }


        // PUT: api/botdatasubmissions/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotDataSubmissionUpdateDto dto)
        {
            var submission = await _context.BotDataSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound();

            submission.BotId = dto.BotId;
            submission.CaptureFieldId = dto.CaptureFieldId;
            submission.SubmissionValue = dto.SubmissionValue;
            submission.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/botdatasubmissions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var submission = await _context.BotDataSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound();

            _context.BotDataSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
