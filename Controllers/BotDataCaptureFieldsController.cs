using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotDataCaptureFieldsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotDataCaptureFieldsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/botdatacapturefields
        [HttpGet]
    [HasPermission("CanViewBotDataCaptureFields")]
    public async Task<ActionResult<IEnumerable<BotDataCaptureFieldResponseDto>>> GetAll()
        {
            var fields = await _context.BotDataCaptureFields
                .Select(f => new BotDataCaptureFieldResponseDto
                {
                    Id = f.Id,
                    BotId = f.BotId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                })
                .ToListAsync();

            return Ok(fields);
        }

        // GET: api/botdatacapturefields/{id}
        [HttpGet("{id}")]
    [HasPermission("CanViewBotDataCaptureFields")]
    public async Task<ActionResult<BotDataCaptureFieldResponseDto>> GetById(int id)
        {
            var field = await _context.BotDataCaptureFields.FindAsync(id);

            if (field == null)
                return NotFound();

            return new BotDataCaptureFieldResponseDto
            {
                Id = field.Id,
                BotId = field.BotId,
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                CreatedAt = field.CreatedAt,
                UpdatedAt = field.UpdatedAt
            };
        }
        // GET: api/botdatacapturefields/by-bot/23
        [HttpGet("by-bot/{botId}")]
    [HasPermission("CanViewBotDataCaptureFields")]
    public async Task<ActionResult<IEnumerable<BotDataCaptureFieldResponseDto>>> GetByBot(int botId)
        {
            var fields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == botId)
                .Select(f => new BotDataCaptureFieldResponseDto
                {
                    Id = f.Id,
                    BotId = f.BotId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                })
                .ToListAsync();

            return Ok(fields);
        }


        // POST: api/botdatacapturefields
        [HttpPost]
        public async Task<ActionResult<BotDataCaptureFieldResponseDto>> Create(BotDataCaptureFieldCreateDto dto)
        {
            // Authorization: allow if user has CanEditBotDataCaptureFields OR if user is owner of the bot
            var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Forbid();
            }

            // Check role permissions
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var hasPermission = user != null && user.Role != null && user.Role.RolePermissions != null && user.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.Name == "CanEditBotDataCaptureFields");

            // Check ownership of the target bot
            var bot = await _context.Bots.FindAsync(dto.BotId);
            var isOwner = bot != null && bot.UserId == userId;

            if (!hasPermission && !isOwner)
            {
                return Forbid();
            }
            var field = new BotDataCaptureField
            {
                BotId = dto.BotId,
                FieldName = dto.FieldName,
                FieldType = dto.FieldType,
                IsRequired = dto.IsRequired,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotDataCaptureFields.Add(field);
            await _context.SaveChangesAsync();

            // Marcar fase 'data_capture' como completada para el bot (non-blocking)
            // Nota: esta marca ocurre aquí porque la intención es que cuando el usuario añade un campo
            // de captura y lo asocia con el bot, se considere esa parte de la captura de datos.
            try
            {
                Console.WriteLine($"[BotPhases] BotDataCaptureFieldsController: upserting data_capture for bot {field.BotId} at {DateTime.UtcNow:o}");
                var meta = System.Text.Json.JsonSerializer.Serialize(new { source = "data_capture_field", fieldId = field.Id });
                var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == field.BotId && p.Phase == "data_capture");
                if (phase == null)
                {
                    _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = field.BotId, Phase = "data_capture", CompletedAt = DateTime.UtcNow, Meta = meta, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
                else
                {
                    phase.CompletedAt = DateTime.UtcNow;
                    phase.Meta = meta;
                    phase.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotPhases] BotDataCaptureFieldsController: failed to upsert data_capture for bot (err): {ex.Message}");
            }

            var responseDto = new BotDataCaptureFieldResponseDto
            {
                Id = field.Id,
                BotId = field.BotId,
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                CreatedAt = field.CreatedAt,
                UpdatedAt = field.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = field.Id }, responseDto);
        }

        // PUT: api/botdatacapturefields/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotDataCaptureFieldUpdateDto dto)
        {
            var field = await _context.BotDataCaptureFields.FindAsync(id);

            if (field == null)
                return NotFound();

            field.BotId = dto.BotId;
            field.FieldName = dto.FieldName;
            field.FieldType = dto.FieldType;
            field.IsRequired = dto.IsRequired;
            field.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/botdatacapturefields/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var field = await _context.BotDataCaptureFields.FindAsync(id);

            if (field == null)
                return NotFound();

            _context.BotDataCaptureFields.Remove(field);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
