using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Voia.Api.Data;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/bottemplates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotTemplateResponseDto>>> GetAll()
        {
            var templates = await _context.BotTemplates
                .Select(t => new BotTemplateResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    IaProviderId = t.IaProviderId,
                    DefaultStyleId = t.DefaultStyleId,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(templates);
        }

        // GET: api/bottemplates/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BotTemplateResponseDto>> GetById(int id)
        {
            var t = await _context.BotTemplates.FindAsync(id);

            if (t == null)
                return NotFound();

            return new BotTemplateResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                IaProviderId = t.IaProviderId,
                DefaultStyleId = t.DefaultStyleId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            };
        }

        // POST: api/bottemplates
        [HttpPost]
        public async Task<ActionResult<BotTemplateResponseDto>> Create(BotTemplateCreateDto dto)
        {
            var template = new BotTemplate
            {
                Name = dto.Name,
                Description = dto.Description,
                IaProviderId = dto.IaProviderId,
                DefaultStyleId = dto.DefaultStyleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotTemplates.Add(template);
            await _context.SaveChangesAsync();

            var responseDto = new BotTemplateResponseDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                IaProviderId = template.IaProviderId,
                DefaultStyleId = template.DefaultStyleId,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = template.Id }, responseDto);
        }

        // PUT: api/bottemplates/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotTemplateUpdateDto dto)
        {
            var template = await _context.BotTemplates.FindAsync(id);

            if (template == null)
                return NotFound();

            template.Name = dto.Name;
            template.Description = dto.Description;
            template.IaProviderId = dto.IaProviderId;
            template.DefaultStyleId = dto.DefaultStyleId;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/bottemplates/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.BotTemplates.FindAsync(id);

            if (template == null)
                return NotFound();

            _context.BotTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
