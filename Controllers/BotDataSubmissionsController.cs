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
            // ✅ OPTIMIZACIÓN: Realizar el agrupamiento y la mayor parte del procesamiento en la base de datos.
            var groupedSubmissions = await _context.BotDataSubmissions
                .Where(s => s.BotId == botId)
                .Include(s => s.CaptureField) // Incluir el campo para obtener el nombre
                .GroupBy(s => s.SubmissionSessionId ?? s.UserId.ToString())
                .Select(g => new
                {
                    SessionKey = g.Key,
                    LastSubmissionDate = g.Max(s => s.SubmittedAt),
                    Submissions = g.Select(s => new { s.CaptureField.FieldName, s.SubmissionValue, s.UserId, s.SubmissionSessionId })
                })
                .OrderByDescending(g => g.LastSubmissionDate)
                .ToListAsync();

            // Mapeo final en memoria
            var result = groupedSubmissions.Select(g => new
            {
                sessionId = g.Submissions.FirstOrDefault()?.SubmissionSessionId,
                userId = g.Submissions.FirstOrDefault()?.UserId,
                values = g.Submissions
                    .GroupBy(s => s.FieldName)
                    .ToDictionary(grp => grp.Key, grp => grp.Select(s => s.SubmissionValue).ToList()),
                createdAt = g.LastSubmissionDate
            });

            return Ok(result);
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
            {
                Console.WriteLine("[BotDataSubmissions][CreateBatch] ❌ ERROR: La lista de submissions es nula o vacía.");
                return BadRequest(new { Message = "La lista de submissions no puede ser nula o vacía." });
            }

            // Log del JSON crudo recibido (solo en modo DEBUG)
#if DEBUG
            try
            {
                Request.EnableBuffering();
                using (var reader = new System.IO.StreamReader(Request.Body, leaveOpen: true))
                {
                    var rawBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"[BotDataSubmissions][CreateBatch] JSON recibido (DEBUG): {rawBody}");
                    Request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotDataSubmissions][CreateBatch] Error leyendo el body para logging: {ex.Message}");
            }
#endif

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in submissions)
                {
                    if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.SubmissionSessionId))
                    {
                        return BadRequest(new { Message = "Cada envío debe tener un userId o submissionSessionId." });
                    }

                    // 🔍 Validar que el CaptureFieldId corresponda al BotId
                    var validField = await _context.BotDataCaptureFields
                        .AnyAsync(f => f.Id == dto.CaptureFieldId && f.BotId == dto.BotId);

                    if (!validField)
                    {
                        Console.WriteLine($"[BotDataSubmissions][CreateBatch] ❌ ERROR: CaptureFieldId={dto.CaptureFieldId} no pertenece al BotId={dto.BotId}");
                        // No es necesario hacer rollback aquí, el catch lo hará si se lanza una excepción.
                        return BadRequest(new { Message = $"El campo {dto.CaptureFieldId} no pertenece al bot {dto.BotId}." });
                    }

                    Console.WriteLine($"[BotDataSubmissions][CreateBatch] Procesando DTO: BotId={dto.BotId}, CaptureFieldId={dto.CaptureFieldId}, Value='{dto.SubmissionValue}', UserId={dto.UserId}, SessionId='{dto.SubmissionSessionId}'");

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
                await transaction.CommitAsync();

                Console.WriteLine($"[BotDataSubmissions][CreateBatch] ✅ {submissions.Count} registros guardados exitosamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[BotDataSubmissions][CreateBatch] ❌ ERROR al guardar en DB: {ex.Message}");
                Console.WriteLine($"[BotDataSubmissions][CreateBatch] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Error interno al guardar los datos.", Error = ex.Message });
            }

            return Ok(new { Message = $"{submissions.Count} registros creados correctamente." });
        }

        // POST: api/botdatasubmissions
        [HttpPost]
        public async Task<ActionResult<BotDataSubmissionResponseDto>> Create([FromBody] BotDataSubmissionCreateDto dto)
        {
            // Log detallado del payload recibido
            Console.WriteLine("[BotDataSubmissions] Payload recibido:");
            try
            {
                // Habilitar buffering para poder leer el body múltiples veces si es necesario
                Request.EnableBuffering();
                
                using (var reader = new System.IO.StreamReader(Request.Body, leaveOpen: true))
                {
                    var rawBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"[BotDataSubmissions] JSON recibido: {rawBody}");
                    // Rebobinar el stream para que el model binding pueda leerlo
                    Request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"[BotDataSubmissions] Error leyendo el body para logging: {ex.Message}");
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

            Console.WriteLine($"[BotDataSubmissions][Create] Procesando DTO: BotId={dto.BotId}, CaptureFieldId={dto.CaptureFieldId}, Value='{dto.SubmissionValue}', UserId={dto.UserId}, SessionId='{dto.SubmissionSessionId}'");

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
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("[BotDataSubmissions][Create] ✅ Datos guardados exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotDataSubmissions][Create] ❌ ERROR al guardar en DB: {ex.Message}");
                Console.WriteLine($"[BotDataSubmissions][Create] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Error interno al guardar el dato.", Error = ex.Message });
            }

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

            // ✅ Validar integridad antes de actualizar
            var validField = await _context.BotDataCaptureFields
                .AnyAsync(f => f.Id == dto.CaptureFieldId && f.BotId == dto.BotId);

            if (!validField)
            {
                return BadRequest(new { Message = $"El campo {dto.CaptureFieldId} no pertenece al bot {dto.BotId}." });
            }

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
