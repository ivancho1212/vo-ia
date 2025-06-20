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
