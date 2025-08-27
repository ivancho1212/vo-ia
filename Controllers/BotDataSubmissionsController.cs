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
                        .GroupBy(g => fieldNames[g.CaptureFieldId])
                        .ToDictionary(
                            gr => gr.Key,
                            gr => gr.Select(x => x.SubmissionValue).ToList()
                        );

                    return new
                    {
                        sessionId,
                        userId,
                        values,
                        createdAt = group.Max(x => x.SubmittedAt) // 👈 última fecha captada
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
                .Where(s => fields.ContainsKey(s.CaptureFieldId))
                .Select(s => new
                {
                    UserId = s.UserId,
                    SessionId = s.SubmissionSessionId,
                    CreatedAt = s.SubmittedAt,
                    Field = fields[s.CaptureFieldId],
                    Value = s.SubmissionValue
                })
                .OrderBy(x => x.SessionId)
                .ThenBy(x => x.CreatedAt)
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
        public async Task<ActionResult<BotDataSubmissionResponseDto>> Create([FromBody] BotDataSubmissionCreateDto dto)
        {
            // Log detallado del payload recibido
            Console.WriteLine("[BotDataSubmissions] Payload recibido:");
            try
            {
                var rawBody = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();
                Console.WriteLine($"[BotDataSubmissions] JSON recibido: {rawBody}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotDataSubmissions] Error leyendo el body: {ex.Message}");
            }
            Console.WriteLine($"BotId: {dto.BotId}, CaptureFieldId: {dto.CaptureFieldId}, SubmissionValue: {dto.SubmissionValue}, UserId: {dto.UserId}, SubmissionSessionId: {dto.SubmissionSessionId}");

            // Validación explícita de campos obligatorios
            if (dto.BotId == 0)
            {
                Console.WriteLine("[BotDataSubmissions] ❌ ERROR: BotId es 0 o no enviado");
                return BadRequest(new { Message = "El campo BotId es obligatorio y debe ser distinto de 0." });
            }
            if (dto.CaptureFieldId == 0)
            {
                Console.WriteLine("[BotDataSubmissions] ❌ ERROR: CaptureFieldId es 0 o no enviado");
                return BadRequest(new { Message = "El campo CaptureFieldId es obligatorio y debe ser distinto de 0." });
            }
            if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.SubmissionSessionId))
            {
                Console.WriteLine("[BotDataSubmissions] ❌ ERROR: Falta userId y submissionSessionId");
                return BadRequest(new
                {
                    Message = "Debe especificar un userId o un submissionSessionId para asociar el origen del dato.",
                    Debug = new
                    {
                        dto.BotId,
                        dto.CaptureFieldId,
                        dto.SubmissionValue,
                        dto.UserId,
                        dto.SubmissionSessionId
                    }
                });
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
