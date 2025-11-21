using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Voia.Api.Services.Caching;

using Voia.Api.Services.Security;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ISanitizationService _sanitizer;

        public BotTemplatesController(ApplicationDbContext context, ICacheService cacheService, ISanitizationService sanitizer)
        {
            _context = context;
            _cacheService = cacheService;
            _sanitizer = sanitizer;
        }

        // GET: api/bottemplates
        [HttpGet]
    [HasPermission("CanViewBotTemplates")]
    public async Task<ActionResult<IEnumerable<BotTemplateResponseDto>>> GetAll()
        {
            var cacheKey = CacheConstants.GetAllTemplatesKey();
            
            // Intentar obtener del caché
            var cached = await _cacheService.GetAsync<List<BotTemplateResponseDto>>(cacheKey);
            if (cached != null)
            {
                return Ok(cached);
            }

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

            // Guardar en caché
            await _cacheService.SetAsync(cacheKey, templates, CacheConstants.TEMPLATE_TTL);

            return Ok(templates);
        }
        // GET: /api/bottemplates/available
        [HttpGet("available")]
    [HasPermission("CanViewBotTemplates")]
    public async Task<ActionResult<IEnumerable<BotTemplate>>> GetAvailableTemplates()
        {
            var templates = await _context.BotTemplates
                .Include(t => t.Prompts)
                .Include(t => t.IaProvider)
                .Include(t => t.AiModelConfig)
                .Include(t => t.DefaultStyle)
                .ToListAsync();

            return Ok(templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                IaProviderName = t.IaProvider.Name,
                AiModelName = t.AiModelConfig.ModelName,
                StyleName = t.DefaultStyle != null ? t.DefaultStyle.Theme : null, // ✅ usa Theme o cualquier otro campo válido

                SystemPrompt = t.Prompts.FirstOrDefault(p => p.Role == PromptRole.system)?.Content ?? ""
            }));
        }

        // GET: api/bottemplates/{id}

        [HttpGet("{id}")]
    [HasPermission("CanViewBotTemplates")]
    public async Task<ActionResult<BotTemplateResponseDto>> GetById(int id)
        {
            try
            {
                var template = await _context.BotTemplates
                    .Include(t => t.Prompts)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (template == null)
                    return NotFound();

                var dto = new BotTemplateResponseDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    IaProviderId = template.IaProviderId,
                    AiModelConfigId = template.AiModelConfigId,
                    DefaultStyleId = template.DefaultStyleId,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                    Prompts = template.Prompts.Select(p => new BotTemplatePromptResponseDto
                    {
                        Id = p.Id,
                        BotTemplateId = p.BotTemplateId,
                        Role = Enum.IsDefined(typeof(PromptRole), p.Role) ? p.Role.ToString() : "undefined",
                        Content = p.Content,
                        // elimina Order si no existe
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                // Log ex.Message o ex.ToString() aquí
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
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
                    Name = _sanitizer.SanitizeText(string.IsNullOrWhiteSpace(dto.Name) ? "Plantilla sin nombre" : dto.Name),
                    Description = _sanitizer.SanitizeText(dto.Description ?? ""),
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
            var template = await _context.BotTemplates
                .Include(t => t.Prompts)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            // Validar AiModelConfigId
            if (dto.AiModelConfigId.HasValue && dto.AiModelConfigId != template.AiModelConfigId)
            {
                var modelConfigExists = await _context.AiModelConfigs
                    .AnyAsync(m => m.Id == dto.AiModelConfigId.Value);
                if (!modelConfigExists)
                    return BadRequest($"AiModelConfigId {dto.AiModelConfigId} no es válido.");

                template.AiModelConfigId = dto.AiModelConfigId.Value;
            }

            // Validar IaProviderId
            if (dto.IaProviderId.HasValue && dto.IaProviderId != template.IaProviderId)
            {
                var providerExists = await _context.BotIaProviders
                    .AnyAsync(p => p.Id == dto.IaProviderId.Value);
                if (!providerExists)
                    return BadRequest($"IaProviderId {dto.IaProviderId} no es válido.");

                template.IaProviderId = dto.IaProviderId.Value;
            }

            // Name y description
            if (!string.IsNullOrWhiteSpace(dto.Name))
                template.Name = _sanitizer.SanitizeText(dto.Name);

            if (dto.Description != null)
                template.Description = _sanitizer.SanitizeText(dto.Description);

            if (dto.DefaultStyleId.HasValue)
                template.DefaultStyleId = dto.DefaultStyleId;

            template.UpdatedAt = DateTime.UtcNow;

            // ACTUALIZAR PROMPTS
            if (dto.Prompts != null)
            {
                // Eliminar prompts existentes
                _context.BotTemplatePrompts.RemoveRange(template.Prompts);

                // Agregar los nuevos
                foreach (var promptDto in dto.Prompts)
                {
                    if (!Enum.TryParse<PromptRole>(promptDto.Role, true, out var role))
                        return BadRequest("Rol de prompt no válido.");


                    template.Prompts.Add(new BotTemplatePrompt
                    {
                        Role = role,
                        Content = _sanitizer.SanitizeText(promptDto.Content),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

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
