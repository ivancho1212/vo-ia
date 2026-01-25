using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using Voia.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Security.Claims;
using Voia.Api.Attributes;
using Voia.Api.Services.Caching;

namespace Voia.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BotStylesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public BotStylesController(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Obtiene todos los estilos de los bots.
        /// </summary>
        [HttpGet]
    [HasPermission("CanViewBotStyles")]
    public async Task<ActionResult<IEnumerable<BotStyle>>> GetAllStyles()
        {
            try
            {
                var cacheKey = CacheConstants.GetAllStylesKey();
                
                // Intentar obtener del caché
                var cached = await _cacheService.GetAsync<List<BotStyle>>(cacheKey);
                if (cached != null)
                {
                    return Ok(cached);
                }

                var styles = await _context.BotStyles.ToListAsync();
                
                // Guardar en caché
                await _cacheService.SetAsync(cacheKey, styles, CacheConstants.STYLE_TTL);
                
                return Ok(styles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Obtiene el estilo de un bot por su ID.
        /// </summary>
        [HttpGet("{id}")]
    [HasPermission("CanViewBotStyles")]
    public async Task<ActionResult<BotStyle>> GetStyleById(int id)
        {
            var style = await _context.BotStyles.FindAsync(id);

            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            return Ok(style);
        }
        /// <summary>
        /// Obtiene todos los estilos de un usuario específico.
        /// </summary>
        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult<IEnumerable<BotStyle>>> GetStylesByUser(int userId)
        {
            try
            {
                // Allow if the caller is the same user (owner) or has the CanViewBotStyles permission
                // Try several common claim names (NameIdentifier, sub, id) to support different JWT issuers
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                                  ?? HttpContext.User.FindFirst("sub")
                                  ?? HttpContext.User.FindFirst("id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int callerId))
                {
                    return Forbid();
                }

                if (callerId != userId)
                {
                    // Caller is requesting someone else's styles: require permission
                    var caller = await _context.Users
                        .Include(u => u.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.Id == callerId);

                    var hasPermission = caller != null && caller.Role != null && caller.Role.RolePermissions != null &&
                        caller.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.Name == "CanViewBotStyles");

                    if (!hasPermission)
                        return Forbid();
                }

                var styles = await _context.BotStyles
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                return Ok(styles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        /// <summary>
        /// Actualiza el estilo de un bot existente.
        /// </summary>
        [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStyle(int id, [FromBody] UpdateBotStyleDto dto)
        {
            try
            {
                var style = await _context.BotStyles.FindAsync(id);

                if (style == null)
                {
                    return NotFound(new { message = "Style not found" });
                }

                // Authorization: allow if caller is the owner of the style, otherwise require CanEditBotStyles permission
                var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? HttpContext.User.FindFirst("sub")
                                  ?? HttpContext.User.FindFirst("id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int callerId))
                {
                    return Forbid();
                }

                if (callerId != style.UserId)
                {
                    // Caller is not the owner: require permission
                    var caller = await _context.Users
                        .Include(u => u.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.Id == callerId);

                    var hasPermission = caller != null && caller.Role != null && caller.Role.RolePermissions != null &&
                        caller.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.Name == "CanUpdateBotStyles");

                    if (!hasPermission)
                        return Forbid();
                }
                // Validaciones adicionales
                if (string.IsNullOrEmpty(dto.Theme))
                    dto.Theme = "light";
                if (string.IsNullOrEmpty(dto.PrimaryColor))
                    dto.PrimaryColor = "#000000";
                if (string.IsNullOrEmpty(dto.SecondaryColor))
                    dto.SecondaryColor = "#ffffff";
                if (string.IsNullOrEmpty(dto.FontFamily))
                    dto.FontFamily = "Arial";
                if (string.IsNullOrEmpty(dto.Position))
                    dto.Position = "bottom-right";

                style.UserId = dto.UserId;
                style.Name = dto.Name;
                style.StyleTemplateId = dto.StyleTemplateId;
                style.Theme = dto.Theme;
                style.PrimaryColor = dto.PrimaryColor;
                style.SecondaryColor = dto.SecondaryColor;
                style.HeaderBackgroundColor = dto.HeaderBackgroundColor;
                style.FontFamily = dto.FontFamily;
                style.AvatarUrl = dto.AvatarUrl ?? "";
                style.Position = dto.Position;
                style.CustomCss = dto.CustomCss ?? "";
                style.Title = dto.Title;
                style.AllowImageUpload = dto.AllowImageUpload;
                style.AllowFileUpload = dto.AllowFileUpload;
                style.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(style);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message 
                });
            }
        }


        /// <summary>
        /// Crea un nuevo estilo para un bot.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStyle([FromBody] CreateBotStyleDto dto)
        {
            try
            {
                var style = new BotStyle
                {
                    UserId = dto.UserId,
                    Name = dto.Name,
                    StyleTemplateId = dto.StyleTemplateId,
                    Theme = dto.Theme,
                    PrimaryColor = dto.PrimaryColor,
                    SecondaryColor = dto.SecondaryColor,
                    HeaderBackgroundColor = dto.HeaderBackgroundColor,
                    FontFamily = dto.FontFamily,
                    AvatarUrl = dto.AvatarUrl,
                    Position = dto.Position,
                    CustomCss = dto.CustomCss,
                    Title = dto.Title,
                    AllowImageUpload = dto.AllowImageUpload,
                    AllowFileUpload = dto.AllowFileUpload,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BotStyles.Add(style);
                await _context.SaveChangesAsync();

                // ✅ Si viene un BotId, asociar el estilo al bot
                if (dto.BotId.HasValue)
                {
                    var bot = await _context.Bots.FindAsync(dto.BotId.Value);
                    if (bot != null)
                    {
                        bot.StyleId = style.Id;
                        await _context.SaveChangesAsync();
                        // Marcar fase 'styles' como completada
                        try
                        {
                            var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == bot.Id && p.Phase == "styles");
                            if (phase == null)
                            {
                                _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = bot.Id, Phase = "styles", CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                            }
                            else
                            {
                                phase.CompletedAt = DateTime.UtcNow;
                                phase.UpdatedAt = DateTime.UtcNow;
                            }
                            await _context.SaveChangesAsync();
                        }
                        catch { }
                    }
                }

                return Ok(style);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        /// <summary>
        /// Elimina un estilo de bot.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStyle(int id)
        {
            var style = await _context.BotStyles.FindAsync(id);

            if (style == null)
            {
                return NotFound(new { message = "Style not found" });
            }

            _context.BotStyles.Remove(style);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Style deleted successfully" });
        }

        /// <summary>
        /// Obtiene el estilo de un bot para widgets
        /// REQUIERE TOKEN VÁLIDO - Si la integración se elimina, rechaza el request
        /// </summary>
        [BotTokenAuthorize] // ✅ CRÍTICO: Valida que la integración existe
        [EnableCors("AllowWidgets")] // 🔧 CORS para widgets
        [HttpGet("widget/{botId}")]
        public async Task<ActionResult<object>> GetStyleForWidget(int botId)
        {
            try
            {
                var bot = await _context.Bots
                    .Include(b => b.Style)
                    .FirstOrDefaultAsync(b => b.Id == botId);

                if (bot?.Style == null)
                {
                    return NotFound(new { message = "Bot or style not found" });
                }

                var response = new
                {
                    styles = new
                    {
                        name = bot.Style.Title ?? bot.Name ?? "Asistente Virtual",
                        primaryColor = bot.Style.PrimaryColor ?? "#000000",
                        secondaryColor = bot.Style.SecondaryColor ?? "#ffffff",
                        headerBackgroundColor = bot.Style.HeaderBackgroundColor ?? "#000000",
                        position = bot.Style.Position ?? "bottom-right",
                        fontFamily = bot.Style.FontFamily ?? "Arial",
                        avatarUrl = bot.Style.AvatarUrl ?? "",
                        isEmojiAvatar = !string.IsNullOrEmpty(bot.Style.AvatarUrl) && 
                                       System.Text.RegularExpressions.Regex.IsMatch(bot.Style.AvatarUrl, @"\p{So}|\p{Cn}"),
                        allowImageUpload = bot.Style.AllowImageUpload,
                        allowFileUpload = bot.Style.AllowFileUpload,
                        theme = bot.Style.Theme ?? "light",
                        title = bot.Style.Title ?? bot.Name ?? "Asistente Virtual"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
