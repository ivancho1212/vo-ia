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
            // Nota: evitar llamadas a ToString() dentro de expresiones LINQ que se traducen a SQL
            // porque EF Core no puede traducir esa llamada y provocará una excepción en tiempo de ejecución.
            // En su lugar agrupamos por un objeto anónimo y realizamos el mapeo final en memoria.
            var groupedSubmissions = await _context.BotDataSubmissions
                .Where(s => s.BotId == botId)
                .Include(s => s.CaptureField) // Incluir el campo para obtener el nombre
                .GroupBy(s => new { s.SubmissionSessionId, s.UserId })
                .Select(g => new
                {
                    Key = g.Key,
                    LastSubmissionDate = g.Max(s => s.SubmittedAt),
                    Submissions = g.Select(s => new { s.CaptureField.FieldName, s.SubmissionValue, s.UserId, s.SubmissionSessionId, s.ConversationId, s.CaptureIntent, s.CaptureSource, s.MetadataJson })
                })
                .OrderByDescending(g => g.LastSubmissionDate)
                .ToListAsync();

            // Mapeo final en memoria
            var result = groupedSubmissions.Select(g => new
            {
                // Ahora Key es un objeto con SubmissionSessionId y UserId
                sessionId = g.Key.SubmissionSessionId,
                userId = g.Key.UserId,
                values = g.Submissions
                    .GroupBy(s => s.FieldName)
                    .ToDictionary(grp => grp.Key, grp => grp.Select(s => s.SubmissionValue).ToList()),
                conversationId = g.Submissions.FirstOrDefault()?.ConversationId,
                captureIntent = g.Submissions.FirstOrDefault()?.CaptureIntent,
                captureSource = g.Submissions.FirstOrDefault()?.CaptureSource,
                metadataJson = g.Submissions.FirstOrDefault()?.MetadataJson,
                createdAt = g.LastSubmissionDate
            });

            return Ok(result);
        }

    // GET: api/BotDataSubmissions/public/by-bot/{botId}

    [HttpGet("public/by-bot/{botId}")]
    [HasPermission("datos_captados_bot")] // Require explicit permission to access public submissions
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
                    Value = s.SubmissionValue,
                    ConversationId = s.ConversationId,
                    CaptureIntent = s.CaptureIntent,
                    CaptureSource = s.CaptureSource,
                    MetadataJson = s.MetadataJson
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
                        ConversationId = dto.ConversationId,
                        CaptureIntent = dto.CaptureIntent,
                        CaptureSource = dto.CaptureSource,
                        MetadataJson = dto.MetadataJson,
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
                ConversationId = dto.ConversationId,
                CaptureIntent = dto.CaptureIntent,
                CaptureSource = dto.CaptureSource,
                MetadataJson = dto.MetadataJson,
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
                SubmittedAt = submission.SubmittedAt,
                ConversationId = submission.ConversationId,
                CaptureIntent = submission.CaptureIntent,
                CaptureSource = submission.CaptureSource,
                MetadataJson = submission.MetadataJson
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

        // GET: api/BotDataSubmissions/export?botId=1&from=2025-01-01&to=2025-12-31&sessionId=...&intent=...
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] int botId, [FromQuery] string? sessionId, [FromQuery] int? userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? intent)
        {
            if (botId == 0)
                return BadRequest(new { Message = "El parámetro botId es obligatorio." });

            var bot = await _context.Bots.FindAsync(botId);
            if (bot == null)
                return NotFound(new { Message = $"Bot {botId} no encontrado." });

            // Permisos: permitir solo al propietario del bot o a administradores/usuarios con permiso
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authUserId))
            {
                return Forbid();
            }

            if (bot.UserId != authUserId)
            {
                // No es el propietario. Comprobar rol/permission
                var user = await _context.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == authUserId);

                if (user == null)
                    return Forbid();

                var isAdmin = !string.IsNullOrEmpty(user.Role?.Name) && user.Role.Name.Equals("admin", System.StringComparison.OrdinalIgnoreCase);
                var hasPermission = user.Role?.RolePermissions?.Any(rp => rp.Permission != null && (rp.Permission.Name == "CanViewBots" || rp.Permission.Name == "CanExportData")) == true;

                if (!isAdmin && !hasPermission)
                {
                    return Forbid();
                }
            }

            // Obtener campos definidos para el bot (ordenados para consistencia de columnas)
            var fields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == botId)
                .OrderBy(f => f.Id)
                .ToListAsync();

            // Filtrar submissions según parámetros
            var q = _context.BotDataSubmissions
                .Where(s => s.BotId == botId);

            if (!string.IsNullOrWhiteSpace(sessionId)) q = q.Where(s => s.SubmissionSessionId == sessionId);
            if (userId.HasValue) q = q.Where(s => s.UserId == userId.Value);
            if (from.HasValue) q = q.Where(s => s.SubmittedAt >= from.Value);
            if (to.HasValue) q = q.Where(s => s.SubmittedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(intent)) q = q.Where(s => s.CaptureIntent == intent);

            var submissions = await q.Include(s => s.CaptureField).ToListAsync();

            // Agrupar por sesión (submissionSessionId) y userId
            var grouped = submissions
                .GroupBy(s => new { s.SubmissionSessionId, s.UserId })
                .OrderByDescending(g => g.Max(s => s.SubmittedAt))
                .ToList();

            // Construir CSV
            var sb = new System.Text.StringBuilder();
            // Header
            var headerCols = new List<string> { "BotId", "BotName", "SessionId", "UserId", "ConversationId", "CaptureIntent", "CaptureSource", "MetadataJson", "LastSubmittedAt" };
            headerCols.AddRange(fields.Select(f => f.FieldName));
            sb.AppendLine(string.Join(",", headerCols.Select(h => '"' + h.Replace("\"", "\"\"") + '"')));

            foreach (var g in grouped)
            {
                var lastSubmitted = g.Max(s => s.SubmittedAt);
                var row = new List<string>
                {
                    botId.ToString(),
                    (bot.Name ?? "").Replace("\"", "\"\""),
                    g.Key.SubmissionSessionId ?? "",
                    g.Key.UserId?.ToString() ?? "",
                    g.Select(s => s.ConversationId).FirstOrDefault()?.ToString() ?? "",
                    g.Select(s => s.CaptureIntent).FirstOrDefault() ?? "",
                    g.Select(s => s.CaptureSource).FirstOrDefault() ?? "",
                    (g.Select(s => s.MetadataJson).FirstOrDefault() ?? "").Replace("\"", "\"\""),
                    lastSubmitted?.ToString("o") ?? ""
                };

                // Añadir columnas por campo
                foreach (var f in fields)
                {
                    var values = g.Where(s => s.CaptureFieldId == f.Id).OrderBy(s => s.SubmittedAt).Select(s => s.SubmissionValue).Where(v => !string.IsNullOrEmpty(v)).ToList();
                    var joined = values.Any() ? string.Join(" | ", values).Replace("\"", "\"\"") : "";
                    row.Add(joined);
                }

                sb.AppendLine(string.Join(",", row.Select(c => '"' + c + '"')));
            }

            // Si se solicita formato xlsx, generar archivo XLSX con ClosedXML
            var format = Request.Query.ContainsKey("format") ? Request.Query["format"].ToString().ToLower() : "csv";
            var fileName = $"captured_bot_{botId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            if (format == "xlsx")
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add("Datos Capturados");
                // Header
                for (int i = 0; i < headerCols.Count; i++)
                {
                    ws.Cell(1, i + 1).Value = headerCols[i];
                }

                int rowIdx = 2;
                foreach (var g in grouped)
                {
                    var lastSubmitted = g.Max(s => s.SubmittedAt);
                    var colIdx = 1;
                    ws.Cell(rowIdx, colIdx++).Value = botId;
                    ws.Cell(rowIdx, colIdx++).Value = bot.Name ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Key.SubmissionSessionId ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Key.UserId?.ToString() ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Select(s => s.ConversationId).FirstOrDefault()?.ToString() ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Select(s => s.CaptureIntent).FirstOrDefault() ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Select(s => s.CaptureSource).FirstOrDefault() ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = g.Select(s => s.MetadataJson).FirstOrDefault() ?? "";
                    ws.Cell(rowIdx, colIdx++).Value = lastSubmitted?.ToString("o") ?? "";

                    foreach (var f in fields)
                    {
                        var values = g.Where(s => s.CaptureFieldId == f.Id).OrderBy(s => s.SubmittedAt).Select(s => s.SubmissionValue).Where(v => !string.IsNullOrEmpty(v)).ToList();
                        var joined = values.Any() ? string.Join(" | ", values) : "";
                        ws.Cell(rowIdx, colIdx++).Value = joined;
                    }

                    rowIdx++;
                }

                using var ms = new System.IO.MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;
                var content = ms.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            fileName = fileName + ".csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // GET: api/botdatasubmissions/by-bot-raw/{botId}
        [HttpGet("by-bot-raw/{botId}")]
        public async Task<IActionResult> GetSubmissionsRaw(int botId)
        {
            var bot = await _context.Bots.FindAsync(botId);
            if (bot == null)
                return NotFound();

            var submissions = await _context.BotDataSubmissions
                .Where(s => s.BotId == botId)
                .Include(s => s.CaptureField)
                .OrderBy(s => s.SubmittedAt)
                .Select(s => new
                {
                    Id = s.Id,
                    BotId = s.BotId,
                    CaptureFieldId = s.CaptureFieldId,
                    FieldName = s.CaptureField != null ? s.CaptureField.FieldName : null,
                    SubmissionValue = s.SubmissionValue,
                    SubmittedAt = s.SubmittedAt,
                    UserId = s.UserId,
                    SubmissionSessionId = s.SubmissionSessionId,
                    ConversationId = s.ConversationId,
                    CaptureIntent = s.CaptureIntent,
                    CaptureSource = s.CaptureSource,
                    MetadataJson = s.MetadataJson
                })
                .ToListAsync();

            return Ok(submissions);
        }

        // GET: api/BotDataSubmissions/public/by-user/{userId}
        // Seguridad: permite acceso si:
        // - Llamador autenticado y su id == userId
        // - Se entrega ?token=... que coincide con users.PublicDataToken
        // - Llamador autenticado tiene permiso 'datos_captados_bot' o es admin
        [HttpGet("public/by-user/{userId}")]
        public async Task<IActionResult> GetPublicSubmissionsByUser(int userId, [FromQuery] string? token)
        {
            // Buscar usuario objetivo
            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null) return NotFound();

            // Determinar caller autenticado (si existe)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value
                               ?? User.FindFirst("id")?.Value;

            int? authUserId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsed)) authUserId = parsed;

            var authorized = false;

            // 1) Si el llamador está autenticado y es el mismo usuario
            if (authUserId.HasValue && authUserId.Value == userId) authorized = true;

            // 2) Si se entrega token que coincide con el token público del usuario
            if (!authorized && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(targetUser.PublicDataToken) && token == targetUser.PublicDataToken)
            {
                authorized = true;
            }

            // 3) Si el llamador autenticado tiene permiso explícito o es admin
            if (!authorized && authUserId.HasValue)
            {
                var caller = await _context.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == authUserId.Value);

                if (caller != null)
                {
                    var isAdmin = !string.IsNullOrEmpty(caller.Role?.Name) && caller.Role.Name.Equals("admin", System.StringComparison.OrdinalIgnoreCase);
                    var hasPermission = caller.Role?.RolePermissions?.Any(rp => rp.Permission != null && rp.Permission.Name == "datos_captados_bot") == true;
                    if (isAdmin || hasPermission) authorized = true;
                }
            }

            if (!authorized) return Forbid();

            // Recuperar bots asociados al usuario
            var botIds = await _context.Bots.Where(b => b.UserId == userId).Select(b => b.Id).ToListAsync();

            if (botIds == null || botIds.Count == 0) return Ok(new List<object>());

            // Obtener campos para todos los bots (para ordenar columnas conservadoramente, usaremos los campos de la primera lista)
            var firstBotId = botIds.First();
            var fields = await _context.BotDataCaptureFields.Where(f => botIds.Contains(f.BotId)).OrderBy(f => f.Id).ToListAsync();

            // Traer todas las submissions para esos bots
            var allSubs = await _context.BotDataSubmissions
                .Where(s => botIds.Contains(s.BotId))
                .Include(s => s.CaptureField)
                .ToListAsync();

            // Agrupar por BotId + SessionId + UserId
            var grouped = allSubs
                .GroupBy(s => new { s.BotId, s.SubmissionSessionId, s.UserId })
                .OrderByDescending(g => g.Max(s => s.SubmittedAt))
                .ToList();

            // Construir respuesta JSON (array de objetos con metadata + values)
            var result = grouped.Select(g => new
            {
                BotId = g.Key.BotId,
                SessionId = g.Key.SubmissionSessionId,
                UserId = g.Key.UserId,
                ConversationId = g.Select(s => s.ConversationId).FirstOrDefault(),
                CaptureIntent = g.Select(s => s.CaptureIntent).FirstOrDefault(),
                CaptureSource = g.Select(s => s.CaptureSource).FirstOrDefault(),
                MetadataJson = g.Select(s => s.MetadataJson).FirstOrDefault(),
                CreatedAt = g.Max(s => s.SubmittedAt),
                Values = g.GroupBy(s => s.CaptureField.FieldName)
                          .ToDictionary(grp => grp.Key ?? "", grp => grp.Select(s => s.SubmissionValue).Where(v => v != null).ToList())
            });

            return Ok(result);
        }
    }
}
