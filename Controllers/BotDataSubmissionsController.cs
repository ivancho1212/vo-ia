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

            var grouped = submissions
                .GroupBy(s => s.SubmissionSessionId ?? $"user-{s.UserId}")
                .Select(group =>
                {
                    string sessionId = group.Key.StartsWith("user-") ? null : group.Key;
                    int? userId = group.Key.StartsWith("user-") ? int.Parse(group.Key.Replace("user-", "")) : (int?)null;

                    var values = group
                        .Where(g => fieldNames.ContainsKey(g.CaptureFieldId))
                        .ToDictionary(
                            g => fieldNames[g.CaptureFieldId],
                            g => g.SubmissionValue
                        );

                    return new
                    {
                        sessionId,
                        userId,
                        values
                    };
                })
                .ToList();

            return Ok(grouped);
        }

        // GET: api/BotDataSubmissions/public/by-bot/{botId}

        [HttpGet("public/by-bot/{botId}")]
        // [AllowAnonymous] // Descomenta si quieres que sea público sin auth
        public async Task<IActionResult> GetPublicSubmissions(int botId /*, [FromQuery] string token */)
        {
            var bot = await _context.Bots.FindAsync(botId);
            if (bot == null)
                return NotFound();

            // 🔒 Validación de token (pendiente si lo necesitas luego)
            // if (!string.IsNullOrEmpty(bot.ApiToken) && bot.ApiToken != token)
            //     return Unauthorized("Token inválido");

            // ✅ Obtener los campos definidos para este bot
            var fields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == botId)
                .ToDictionaryAsync(f => f.Id, f => f.FieldName);

            // ✅ Traer las submissions a memoria
            var allSubmissions = await _context.BotDataSubmissions
                .Where(s => s.BotId == botId)
                .ToListAsync();

            // ✅ Agrupar en memoria
            var grouped = allSubmissions
                .GroupBy(s => new { s.UserId, s.SubmissionSessionId })
                .Select(g => new Voia.Api.Dtos.Bot.BotDataGroupedSubmissionDto
                {
                    UserId = g.Key.UserId,
                    SessionId = g.Key.SubmissionSessionId,
                    CreatedAt = g.Max(x => x.SubmittedAt),
                    Values = g
                        .Where(s => fields.ContainsKey(s.CaptureFieldId))
                        .ToDictionary(
                            s => fields[s.CaptureFieldId],
                            s => s.SubmissionValue ?? ""
                        )
                })
                .ToList();

            return Ok(grouped);
        }

        // POST: api/botdatasubmissions/batch
        [HttpPost("batch")]
        public async Task<ActionResult> CreateBatch([FromBody] List<BotDataSubmissionCreateDto> submissions)
        {
            if (submissions == null || !submissions.Any())
                return BadRequest(new { Message = "No se enviaron datos." });

            foreach (var dto in submissions)
            {
                if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.SubmissionSessionId))
                {
                    return BadRequest(new { Message = "Cada envío debe tener un userId o submissionSessionId." });
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
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Datos enviados correctamente." });
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
