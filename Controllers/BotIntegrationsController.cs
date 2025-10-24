using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.BotIntegrations;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // NOTA: Este controlador es para el DASHBOARD, no para el widget. 
    // Debería usar la autorización normal de usuario/admin, no [BotTokenAuthorize].
    // [Authorize(Roles = "Admin,User")] 
    public class BotIntegrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public BotIntegrationsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/botintegrations/with-script
        // Devuelve los bots que ya tienen una integración (script/token) generada.
        [HttpGet("with-script")]
        [HasPermission("CanViewBotIntegrations")]
        public async Task<ActionResult<IEnumerable<object>>> GetBotsWithScript()
        {
            // Traer todas las integraciones y luego los bots asociados
            var integrations = await _context.BotIntegrations.ToListAsync();
            var botIds = integrations.Select(i => i.BotId).Distinct().ToList();

            var bots = await _context.Bots
                .Where(b => botIds.Contains(b.Id))
                .ToListAsync();

            var result = bots.Select(b =>
            {
                var integ = integrations.FirstOrDefault(i => i.BotId == b.Id);
                // Devolver la configuración cruda de settings (frontend puede parsear JSON)
                return new
                {
                    botId = b.Id,
                    name = b.Name,
                    description = b.Description,
                    styleId = b.StyleId,
                    integration = integ == null ? null : new
                    {
                        apiToken = integ.ApiTokenHash,
                        settingsJson = integ.SettingsJson,
                        integrationType = integ.IntegrationType,
                        framework = integ.Framework
                    }
                };
            }).ToList();

            return Ok(result);
        }

    [HttpGet("bot/{botId}")]
    [HasPermission("CanViewBotIntegrations")]
    public async Task<ActionResult<BotIntegrationDto>> GetByBotId(int botId)
        {
            var integration = await _context.BotIntegrations
                .FirstOrDefaultAsync(b => b.BotId == botId);

            if (integration == null)
                return NotFound(new { message = "Bot integration not found for the specified botId." });

            // Deserializar el JSON de settings para obtener el dominio
            var settings = JsonSerializer.Deserialize<WidgetSettings>(integration.SettingsJson ?? "{}");

            var dto = new BotIntegrationDto
            {
                BotId = integration.BotId,
                AllowedDomain = settings?.AllowedDomain,
                ApiToken = integration.ApiTokenHash // El frontend espera 'apiToken'
            };

            // intentar obtener framework de la columna si existe
            dto.Framework = integration.Framework;

            return Ok(dto);
        }

    [HttpPut("upsert")]
    [HasPermission("CanEditBotIntegrations")]
    public async Task<ActionResult<object>> Upsert([FromBody] UpsertIntegrationRequest dto)
        {
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
                return BadRequest(new { message = "BotId does not exist in the system." });

            var integration = await _context.BotIntegrations.FirstOrDefaultAsync(b => b.BotId == dto.BotId);

            // Validar y serializar settings
                var dtoSettingsAllowedDomain = dto.Settings?.AllowedDomain ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dtoSettingsAllowedDomain))
                    return BadRequest(new { message = "AllowedDomain is required for widget integrations." });

            var settingsJson = JsonSerializer.Serialize(dto.Settings);

            // Generar siempre un nuevo token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"] ?? string.Empty;
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("botId", dto.BotId.ToString()),
                    new Claim("allowedDomain", dto.Settings?.AllowedDomain?.Trim() ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddHours(2), // Expiración corta
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            if (integration != null)
            {
                // Actualizar la integración existente
                integration.IntegrationType = "Widget";
                integration.SettingsJson = settingsJson;
                integration.ApiTokenHash = tokenString; // Guardar el nuevo token
                integration.Framework = dto.Framework;

                await _context.SaveChangesAsync();

                // Marcar fase de integración como completada
                try
                {
                    var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == dto.BotId && p.Phase == "integration");
                    if (phase == null)
                    {
                        _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = dto.BotId, Phase = "integration", CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                    }
                    else
                    {
                        phase.CompletedAt = DateTime.UtcNow;
                        phase.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }
                catch { /* non-blocking */ }
                return Ok(new { apiToken = tokenString });
            }
            else
            {
                // Crear una nueva integración
                var newIntegration = new BotIntegration
                {
                    BotId = dto.BotId,
                    IntegrationType = "Widget",
                    SettingsJson = settingsJson,
                    ApiTokenHash = tokenString, // Guardar el nuevo token
                    Framework = dto.Framework,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BotIntegrations.Add(newIntegration);
                await _context.SaveChangesAsync();

                // Marcar fase de integración como completada
                try
                {
                    _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = dto.BotId, Phase = "integration", CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                    await _context.SaveChangesAsync();
                }
                catch { /* non-blocking */ }
                return Ok(new { apiToken = tokenString });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var integration = await _context.BotIntegrations.FindAsync(id);
            if (integration == null)
                return NotFound(new { message = "Bot integration not found." });

            _context.BotIntegrations.Remove(integration);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("bot/{botId}")]
        public async Task<IActionResult> DeleteByBotId(int botId)
        {
            var integration = await _context.BotIntegrations
                .FirstOrDefaultAsync(b => b.BotId == botId);
            
            if (integration == null)
                return NotFound(new { message = "Bot integration not found for the specified botId." });

            _context.BotIntegrations.Remove(integration);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("generate-widget-token")]
        public async Task<ActionResult<object>> GenerateWidgetToken([FromBody] GenerateWidgetTokenRequest request)
        {
            // Verificar que el bot existe
            var botExists = await _context.Bots.AnyAsync(b => b.Id == request.BotId);
            if (!botExists)
                return BadRequest(new { message = "BotId does not exist in the system." });

            // Para desarrollo, usar localhost como dominio permitido
            var allowedDomain = request.AllowedDomain ?? "localhost";

            // Generar token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey2 = _config["Jwt:Key"] ?? string.Empty;
            var key = Encoding.ASCII.GetBytes(jwtKey2);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("botId", request.BotId.ToString()),
                    new Claim("allowedDomain", allowedDomain)
                }),
                Expires = DateTime.UtcNow.AddYears(1), // Token de larga duración para widgets
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Marcar fase 'integration' como completada cuando se genera el script/token
            try
            {
                var meta = System.Text.Json.JsonSerializer.Serialize(new { source = "integration_script", allowedDomain = allowedDomain });
                var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == request.BotId && p.Phase == "integration");
                if (phase == null)
                {
                    _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = request.BotId, Phase = "integration", CompletedAt = DateTime.UtcNow, Meta = meta, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
                else
                {
                    phase.CompletedAt = DateTime.UtcNow;
                    phase.Meta = meta;
                    phase.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
            catch { /* non-blocking */ }

            return Ok(new { 
                token = tokenString,
                botId = request.BotId,
                allowedDomain = allowedDomain,
                expiresAt = DateTime.UtcNow.AddYears(1)
            });
        }
    }

    // DTOs para el request
    public class UpsertIntegrationRequest
    {
        public int BotId { get; set; }
        public WidgetSettings? Settings { get; set; }
        public string? Framework { get; set; }
    }

    public class WidgetSettings
    {
        public string AllowedDomain { get; set; } = string.Empty;
    }

    public class GenerateWidgetTokenRequest
    {
        public int BotId { get; set; }
        public string? AllowedDomain { get; set; }
    }

    public class BotIntegrationDto
    {
        public int BotId { get; set; }
        public string AllowedDomain { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string? Framework { get; set; }
    }
}