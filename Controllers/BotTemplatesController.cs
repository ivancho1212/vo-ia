using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;

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
            var templates = await _context
                .BotTemplates.Select(t => new BotTemplateResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    IaProviderId = t.IaProviderId,
                    DefaultStyleId = t.DefaultStyleId,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
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
                UpdatedAt = t.UpdatedAt,
            };
        }

        // POST: api/bottemplates
        [HttpPost]
        public async Task<ActionResult<BotTemplateResponseDto>> Create(BotTemplateCreateDto dto)
        {
            try
            {
                // Validar IaProviderId
                var providerExists = await _context.BotIaProviders
                    .AnyAsync(p => p.Id == dto.IaProviderId);
                if (!providerExists)
                    return BadRequest($"IaProviderId {dto.IaProviderId} no es válido.");

                // Validar AiModelConfigId
                var modelConfigExists = await _context.AiModelConfigs
                    .AnyAsync(m => m.Id == dto.AiModelConfigId);
                if (!modelConfigExists)
                    return BadRequest($"AiModelConfigId {dto.AiModelConfigId} no es válido.");

                // Validar DefaultStyleId (si viene)
                if (dto.DefaultStyleId.HasValue)
                {
                    var styleExists = await _context.BotStyles
                        .AnyAsync(s => s.Id == dto.DefaultStyleId.Value);
                    if (!styleExists)
                        return BadRequest($"DefaultStyleId {dto.DefaultStyleId} no es válido.");
                }

                var template = new BotTemplate
                {
                    Name = string.IsNullOrWhiteSpace(dto.Name) ? "Plantilla sin nombre" : dto.Name,
                    Description = dto.Description ?? "",
                    IaProviderId = dto.IaProviderId,
                    AiModelConfigId = dto.AiModelConfigId,
                    DefaultStyleId = dto.DefaultStyleId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.BotTemplates.Add(template);
                await _context.SaveChangesAsync();

                var responseDto = new BotTemplateResponseDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    IaProviderId = template.IaProviderId,
                    AiModelConfigId = template.AiModelConfigId,
                    DefaultStyleId = template.DefaultStyleId,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                };

                return CreatedAtAction(nameof(GetById), new { id = template.Id }, responseDto);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner Exception: " + ex.InnerException.Message;
                }
                return StatusCode(500, new { message = errorMessage, stackTrace = ex.StackTrace });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BotTemplateUpdateDto dto)
        {
            var template = await _context.BotTemplates.FindAsync(id);

            if (template == null)
                return NotFound();

            // Validar AiModelConfigId si viene en el DTO
            if (dto.AiModelConfigId.HasValue && dto.AiModelConfigId != template.AiModelConfigId)
            {
                var modelConfigExists = await _context.AiModelConfigs
                    .AnyAsync(m => m.Id == dto.AiModelConfigId.Value);
                if (!modelConfigExists)
                    return BadRequest($"AiModelConfigId {dto.AiModelConfigId} no es válido.");

                template.AiModelConfigId = dto.AiModelConfigId.Value;
            }

            // Validar IaProviderId si viene en el DTO
            if (dto.IaProviderId.HasValue && dto.IaProviderId != template.IaProviderId)
            {
                var providerExists = await _context.BotIaProviders
                    .AnyAsync(p => p.Id == dto.IaProviderId.Value);
                if (!providerExists)
                    return BadRequest($"IaProviderId {dto.IaProviderId} no es válido.");

                template.IaProviderId = dto.IaProviderId.Value;
            }

            // Actualizar Name si viene
            if (!string.IsNullOrWhiteSpace(dto.Name))
                template.Name = dto.Name;

            // Actualizar Description (puede ser string vacío, pero no null)
            if (dto.Description != null)
                template.Description = dto.Description;

            // Actualizar DefaultStyleId si viene (nullable)
            if (dto.DefaultStyleId.HasValue)
                template.DefaultStyleId = dto.DefaultStyleId;

            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new BotTemplateResponseDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                IaProviderId = template.IaProviderId,
                AiModelConfigId = template.AiModelConfigId,
                DefaultStyleId = template.DefaultStyleId,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            });
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
