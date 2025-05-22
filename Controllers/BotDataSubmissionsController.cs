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

        // POST: api/botdatasubmissions
        [HttpPost]
        public async Task<ActionResult<BotDataSubmissionResponseDto>> Create(BotDataSubmissionCreateDto dto)
        {
            var submission = new BotDataSubmission
            {
                BotId = dto.BotId,
                CaptureFieldId = dto.CaptureFieldId,
                SubmissionValue = dto.SubmissionValue,
                SubmittedAt = DateTime.UtcNow
            };

            _context.BotDataSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            var responseDto = new BotDataSubmissionResponseDto
            {
                Id = submission.Id,
                BotId = submission.BotId,
                CaptureFieldId = submission.CaptureFieldId,
                SubmissionValue = submission.SubmissionValue,
                SubmittedAt = submission.SubmittedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = submission.Id }, responseDto);
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
