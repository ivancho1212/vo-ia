using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Claims;
using Voia.Api.Models.Bots;
using Voia.Api.Attributes;
using Voia.Api.Models.Bots;
using Voia.Api.Models.BotTrainingSession;
using Voia.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Voia.Api.Controllers
{

    // [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FastApiService _fastApiService;
        private readonly VectorSearchService _vectorSearch;
        private readonly IConfiguration _config;

        public BotsController(ApplicationDbContext context, FastApiService fastApiService, VectorSearchService vectorSearch, IConfiguration config)
        {
            _context = context;
            _fastApiService = fastApiService;
            _vectorSearch = vectorSearch;
            _config = config;
        }

        /// <summary>
        /// Elimina completamente un bot y todos sus datos asociados (rollback total).
        /// </summary>
        /// <param name="id">ID del bot a eliminar completamente.</param>
        /// <returns>Mensaje de confirmación.</returns>
        [HttpPost("{id}/full-rollback")]
        [HasPermission("CanEditBots")]
        public async Task<IActionResult> FullRollbackBot(int id)
        {
            // Obtener el ID del usuario autenticado
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Usuario no autenticado." });

            // Buscar el bot y validar que pertenezca al usuario
            var bot = await _context.Bots.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (bot == null)
                return NotFound(new { message = "Bot no encontrado o no autorizado." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Eliminar SOLO los documentos asociados a este bot
                var docs = _context.UploadedDocuments.Where(d => d.BotId == id).ToList();
                // Si tienes archivos físicos, aquí podrías eliminarlos del disco SOLO si no están asociados a otro bot
                // foreach (var doc in docs) { /* Eliminar archivo físico si aplica */ }
                _context.UploadedDocuments.RemoveRange(docs);

                // Eliminar SOLO los textos asociados a este bot
                var texts = _context.TrainingCustomTexts.Where(t => t.BotId == id).ToList();
                _context.TrainingCustomTexts.RemoveRange(texts);

                // Eliminar SOLO las urls asociadas a este bot
                var urls = _context.TrainingUrls.Where(u => u.BotId == id).ToList();
                _context.TrainingUrls.RemoveRange(urls);

                // Eliminar SOLO las sesiones de entrenamiento de este bot
                var sessions = _context.BotTrainingSessions.Where(s => s.BotId == id).ToList();
                _context.BotTrainingSessions.RemoveRange(sessions);

                // Eliminar SOLO los prompts custom de este bot
                var prompts = _context.BotCustomPrompts.Where(p => p.BotId == id).ToList();
                _context.BotCustomPrompts.RemoveRange(prompts);

                // Eliminar el bot
                _context.Bots.Remove(bot);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Bot y todos sus datos asociados eliminados completamente.", botId = id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Error al eliminar completamente el bot.", error = ex.Message });
            }
        }


        /// <summary>
        /// Obtiene todos los bots filtrados por nombre o estado.
        /// </summary>
        /// <param name="isActive">Filtra por estado activo o no activo.</param>
        /// <param name="name">Filtra por el nombre del bot.</param>
        /// <returns>Lista de bots.</returns>
        /// <response code="200">Devuelve una lista de bots.</response>
        /// <response code="500">Si ocurre un error interno.</response>
        [HttpGet]
        [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetBots([FromQuery] bool? isActive, [FromQuery] string? name = null)
        {
            try
            {
                var query = _context.Bots
                    .Include(b => b.User) // Incluye la relación User para que no sea null
                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(b => b.IsActive == isActive.Value);
                }

                if (!string.IsNullOrEmpty(name))
                {
                    query = query.Where(b => b.Name.Contains(name));
                }

                var bots = await query.ToListAsync();

                if (bots.Count == 0)
                {
                    return Ok(new { Message = "No bots found" });
                }

                return Ok(bots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }

        [HttpGet("me")]
        [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetMyBots()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var bots = await _context.Bots
                    .Include(b => b.User)
                    .Include(b => b.Style)
                    .Where(b => b.UserId == userId && b.IsActive)
                    .ToListAsync();

                // Project to a minimal DTO so the client reliably receives styleId / style
                var result = bots.Select(b => new {
                    id = b.Id,
                    name = b.Name,
                    description = b.Description,
                    apiKey = b.ApiKey,
                    isActive = b.IsActive,
                    createdAt = b.CreatedAt,
                    updatedAt = b.UpdatedAt,
                    isReady = b.IsReady,
                    userId = b.UserId,
                    botTemplateId = b.BotTemplateId,
                    styleId = b.StyleId,
                    style = b.Style == null ? null : new {
                        id = b.Style.Id,
                        title = b.Style.Title,
                        primaryColor = b.Style.PrimaryColor,
                    }
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene todos los bots de un usuario específico por su ID.
        /// </summary>
        /// <param name="userId">ID del usuario.</param>
        /// <returns>Lista de bots.</returns>
        /// <response code="200">Devuelve los bots encontrados.</response>
        /// <response code="404">Si no se encuentran bots.</response>
        [HttpGet("byUser/{userId}")]
        [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetBotsByUserId(int userId)
        {
            try
            {
                var bots = await _context.Bots
                    .Include(b => b.User)
                    .Where(b => b.UserId == userId && b.IsActive)
                    .ToListAsync();

                if (bots == null || bots.Count == 0)
                {
                    return NotFound(new { Message = "No se encontraron bots para el usuario." });
                }

                return Ok(bots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Ocurrió un error interno", Details = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo bot.
        /// </summary>
        /// <param name="botDto">Datos del bot a crear.</param>
        /// <returns>El bot creado.</returns>
        /// <response code="201">Devuelve el bot creado.</response>
        /// <response code="400">Si los datos son inválidos o el bot ya existe.</response>
        [HasPermission("CanEditBots")]
        [HttpPost]
        public async Task<ActionResult<Bot>> CreateBot([FromBody] CreateBotDto botDto)
        {
            Console.WriteLine("[DEBUG] CreateBot iniciado.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Message = "No se pudo identificar el usuario autenticado." });
            }

            Console.WriteLine($"[DEBUG] UserId autenticado en CreateBot: {userId}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Validar si ya existe un bot con ese nombre antes de crear
                var existingBot = await _context.Bots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Name == botDto.Name && b.UserId == userId && !b.IsDeleted);

                if (existingBot != null)
                    return BadRequest(new { Message = "Ya existe un bot con el mismo nombre." });

                // 2️⃣ Validar plantilla
                var template = await _context.BotTemplates
                    .Include(t => t.DefaultStyle)
                    .FirstOrDefaultAsync(t => t.Id == botDto.BotTemplateId);

                if (template == null)
                    return BadRequest(new { Message = "Plantilla no encontrada." });

                if (template.IaProviderId == 0 || template.AiModelConfigId == 0)
                    return BadRequest(new { Message = "La plantilla no tiene configuración de IA válida." });

                // 3️⃣ Preparar o reutilizar estilo
                int? reusedOrClonedStyleId = null;

                if (template.DefaultStyleId.HasValue)
                {
                    var styleTemplate = await _context.StyleTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == template.DefaultStyleId.Value);

                    if (styleTemplate != null)
                    {
                        string sanitizedPosition = (styleTemplate.Position ?? "bottom-right")
                            .Trim()
                            .Where(c => c >= 32 && c <= 126)
                            .Aggregate("", (acc, c) => acc + c);

                        var existingStyle = await _context.BotStyles
                            .FirstOrDefaultAsync(s =>
                                s.StyleTemplateId == styleTemplate.Id &&
                                s.Theme == (styleTemplate.Theme ?? "light") &&
                                s.PrimaryColor == (styleTemplate.PrimaryColor ?? "#000000") &&
                                s.SecondaryColor == (styleTemplate.SecondaryColor ?? "#ffffff") &&
                                s.FontFamily == (styleTemplate.FontFamily ?? "Arial") &&
                                s.AvatarUrl == (styleTemplate.AvatarUrl ?? "") &&
                                s.Position == sanitizedPosition &&
                                s.CustomCss == (styleTemplate.CustomCss ?? "")
                            );

                        if (existingStyle != null)
                        {
                            reusedOrClonedStyleId = existingStyle.Id;
                        }
                        else
                        {
                            var newStyle = new BotStyle
                            {
                                StyleTemplateId = styleTemplate.Id,
                                Theme = styleTemplate.Theme ?? "light",
                                PrimaryColor = styleTemplate.PrimaryColor ?? "#000000",
                                SecondaryColor = styleTemplate.SecondaryColor ?? "#ffffff",
                                FontFamily = styleTemplate.FontFamily ?? "Arial",
                                AvatarUrl = styleTemplate.AvatarUrl ?? "",
                                Position = sanitizedPosition,
                                CustomCss = styleTemplate.CustomCss ?? "",
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.BotStyles.Add(newStyle);
                            await _context.SaveChangesAsync();
                            reusedOrClonedStyleId = newStyle.Id;
                        }
                    }
                }

                // 4️⃣ Crear bot
                var bot = new Bot
                {
                    Name = botDto.Name,
                    Description = botDto.Description,
                    ApiKey = botDto.ApiKey,
                    IsActive = botDto.IsActive,
                    UserId = userId,
                    BotTemplateId = template.Id,
                    IaProviderId = template.IaProviderId,
                    AiModelConfigId = template.AiModelConfigId,
                    StyleId = reusedOrClonedStyleId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Bots.Add(bot);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Bot creado con Id: {bot.Id}");

                // 5️⃣ Crear sesión de entrenamiento
                var trainingSession = new BotTrainingSession
                {
                    BotId = bot.Id,
                    SessionName = $"Entrenamiento inicial para {botDto.Name}",
                    Description = $"Sesión creada automáticamente al crear el bot desde la plantilla '{template.Name}'",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.BotTrainingSessions.Add(trainingSession);

                // 6️⃣ Heredar documentos, URLs y textos
                var documentos = await _context.UploadedDocuments
                    .Where(d => d.BotTemplateId == template.Id && d.BotId == 0)
                    .ToListAsync();
                documentos.ForEach(d => d.BotId = bot.Id);

                var urls = await _context.TrainingUrls
                    .Where(u => u.BotTemplateId == template.Id && (u.BotId == null || u.BotId == 0))
                    .ToListAsync();
                urls.ForEach(u => u.BotId = bot.Id);

                var textos = await _context.TrainingCustomTexts
                    .Where(t => t.BotTemplateId == template.Id && t.BotId == 0)
                    .ToListAsync();
                textos.ForEach(t => t.BotId = bot.Id);

                // 7️⃣ Agregar texto plano adicional si se envió
                if (!string.IsNullOrWhiteSpace(botDto.CustomText))
                {
                    var customText = new TrainingCustomText
                    {
                        BotId = bot.Id,
                        BotTemplateId = template.Id,
                        TemplateTrainingSessionId = trainingSession.Id,
                        UserId = userId,
                        Content = botDto.CustomText,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.TrainingCustomTexts.Add(customText);
                }

                // 8️⃣ Guardar todo de una vez
                await _context.SaveChangesAsync();

                // 9️⃣ Trigger a la API de vectorización (FastAPI)
                if (documentos.Any())
                    await _fastApiService.TriggerDocumentProcessingAsync(bot.Id);

                if (urls.Any())
                    await _fastApiService.TriggerUrlProcessingAsync(bot.Id);

                if (textos.Any() || !string.IsNullOrWhiteSpace(botDto.CustomText))
                    await _fastApiService.TriggerCustomTextProcessingAsync(bot.Id);

                await transaction.CommitAsync();
                Console.WriteLine("[DEBUG] Bot creado exitosamente con todos los datos heredados.");

                return CreatedAtAction(nameof(GetBotById), new { id = bot.Id }, bot);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("[ERROR] CreateBot falló: " + ex.ToString());
                return StatusCode(500, new { Message = "Error interno al crear el bot." });
            }
        }
        [HttpPatch("{id}/style")]
        public async Task<IActionResult> UpdateBotStyle(int id, [FromBody] UpdateBotStyleSimpleDto dto)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null) return NotFound(new { Message = "Bot not found" });

            bot.StyleId = dto.StyleId;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Marcar fase 'styles' como completada para el bot (non-blocking)
            try
            {
                var meta = System.Text.Json.JsonSerializer.Serialize(new { source = "style_assign", styleId = dto.StyleId });
                var phase = await _context.BotPhases.FirstOrDefaultAsync(p => p.BotId == bot.Id && p.Phase == "styles");
                if (phase == null)
                {
                    _context.BotPhases.Add(new Voia.Api.Models.Bots.BotPhase { BotId = bot.Id, Phase = "styles", CompletedAt = DateTime.UtcNow, Meta = meta, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
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

            return Ok(bot);
        }


        /// <summary>
        /// Actualiza la información de un bot existente.
        /// </summary>
        /// <param name="id">ID del bot a actualizar.</param>
        /// <param name="botDto">Datos para actualizar el bot.</param>
        /// <returns>El bot actualizado.</returns>
        /// <response code="200">Devuelve el bot actualizado.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpPut("{id}")]
        [HasPermission("CanEditBots")]
        public async Task<IActionResult> UpdateBot(int id, [FromBody] UpdateBotDto botDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            bot.Name = botDto.Name;
            bot.Description = botDto.Description;
            bot.ApiKey = botDto.ApiKey;
            bot.IsActive = botDto.IsActive;
            bot.StyleId = botDto.StyleId;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(bot);
        }

        /// <summary>
        /// Elimina un bot (soft delete).
        /// </summary>
        /// <param name="id">ID del bot a eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="200">Bot desactivado correctamente.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpDelete("{id}")]
        [HasPermission("CanDeleteBots")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            // Obtener el ID del usuario autenticado
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Usuario no autenticado." });

            // Buscar el bot y validar que pertenezca al usuario
            var bot = await _context.Bots.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (bot == null)
                return NotFound(new { message = "Bot no encontrado o no autorizado." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bot.IsDeleted = true;
                bot.IsActive = false;
                bot.UpdatedAt = DateTime.UtcNow;
                _context.Bots.Update(bot);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Bot eliminado correctamente (soft delete).",
                    botId = id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Error al eliminar el bot.", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un bot por su ID.
        /// </summary>
        /// <param name="id">ID del bot.</param>
        /// <returns>Bot encontrado.</returns>
        /// <response code="200">Devuelve el bot encontrado.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [AllowAnonymous]
        [EnableCors("AllowWidgets")] // 🔧 CORS para widgets
        [HttpGet("{id}")]
        // [HasPermission("CanViewBot")]
        public async Task<ActionResult<object>> GetBotById(int id)
        {
            var bot = await _context.Bots
                .Include(b => b.User)
                .Include(b => b.TrainingSessions)
                .Include(b => b.Style)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            // Obtener la última sesión de entrenamiento (si existe)
            var lastSession = bot.TrainingSessions?.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            return Ok(new {
                id = bot.Id,
                name = bot.Name,
                description = bot.Description,
                apiKey = bot.ApiKey,
                isActive = bot.IsActive,
                createdAt = bot.CreatedAt,
                updatedAt = bot.UpdatedAt,
                isDeleted = bot.IsDeleted,
                isReady = bot.IsReady,
                userId = bot.UserId,
                botTemplateId = bot.BotTemplateId,
                templateTrainingSessionId = lastSession?.Id,
                // expose the StyleId so frontend can detect assigned style
                styleId = bot.StyleId,
                // minimal style object useful for UI (null-safe)
                style = bot.Style == null ? null : new {
                    id = bot.Style.Id,
                    title = bot.Style.Title,
                    primaryColor = bot.Style.PrimaryColor,
                    secondaryColor = bot.Style.SecondaryColor,
                    avatarUrl = bot.Style.AvatarUrl,
                    position = bot.Style.Position,
                    fontFamily = bot.Style.FontFamily,
                    theme = bot.Style.Theme,
                    customCss = bot.Style.CustomCss,
                    headerBackgroundColor = bot.Style.HeaderBackgroundColor,
                    allowImageUpload = bot.Style.AllowImageUpload,
                    allowFileUpload = bot.Style.AllowFileUpload
                }
            });
        }
        [HttpGet("{id}/widget-settings")]
        [AllowAnonymous] // Permitir acceso sin autenticación
        [EnableCors("AllowWidgets")] // 🔧 CORS para widgets
        // Validación manual de token en el código, no con atributo
        public async Task<ActionResult<WidgetSettingsDto>> GetWidgetSettings(int id, [FromQuery] string token)
        {
            try
            {
                // Primero intentar validar como JWT
                if (!string.IsNullOrEmpty(token) && token.Contains("."))
                {
                    try
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var jwtKey = _config["Jwt:Key"] ?? string.Empty;
                        var key = Encoding.ASCII.GetBytes(jwtKey);

                        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = true,
                            ValidIssuer = _config["Jwt:Issuer"],
                            ValidateAudience = true,
                            ValidAudience = _config["Jwt:Audience"],
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        }, out SecurityToken validatedToken);

                        // Extraer el botId del token
                        var botIdClaim = principal.FindFirst("botId");
                        if (botIdClaim == null || !int.TryParse(botIdClaim.Value, out int tokenBotId) || tokenBotId != id)
                        {
                            return Unauthorized(new { Message = "Invalid token for this bot" });
                        }

                        // Token JWT válido y corresponde a este bot
                        var bot = await _context.Bots
                            .Include(b => b.Style)
                            .FirstOrDefaultAsync(b => b.Id == id);

                        if (bot == null)
                            return NotFound(new { Message = "Bot not found" });

                        var style = bot.Style;
                        var settings = new WidgetSettingsDto
                        {
                            Styles = new StyleSettings
                            {
                                LauncherBackground = style?.PrimaryColor ?? "#000000",
                                HeaderBackground = style?.PrimaryColor ?? "#000000",
                                HeaderText = "#FFFFFF",
                                UserMessageBackground = style?.SecondaryColor ?? "#0084ff",
                                UserMessageText = "#FFFFFF",
                                ResponseMessageBackground = "#f4f7f9",
                                ResponseMessageText = "#000000",
                                Title = style?.Title ?? bot.Name ?? "Asistente Virtual",
                                Subtitle = "Powered by Voia",
                                AvatarUrl = style?.AvatarUrl,
                                Position = style?.Position ?? "bottom-right",
                                FontFamily = style?.FontFamily ?? "Arial",
                                Theme = style?.Theme ?? "light",
                                CustomCss = style?.CustomCss,
                                HeaderBackgroundColor = style?.HeaderBackgroundColor,
                                AllowImageUpload = style?.AllowImageUpload ?? true,
                                AllowFileUpload = style?.AllowFileUpload ?? true
                            },
                            WelcomeMessage = "¡Hola! ¿En qué puedo ayudarte?"
                        };

                        return Ok(settings);
                    }
                    catch (SecurityTokenException)
                    {
                        // JWT inválido, intentar con ApiTokenHash
                    }
                }

                // Fallback: validar como ApiTokenHash (para compatibilidad hacia atrás)
                var bot2 = await _context.Bots
                    .Include(b => b.Style)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bot2 == null)
                    return NotFound(new { Message = "Bot not found" });

                var integration = await _context.BotIntegrations
                    .FirstOrDefaultAsync(bi => bi.BotId == id && bi.ApiTokenHash == token);

                if (integration == null)
                    return Unauthorized(new { Message = "Invalid token for this bot" });

                var style2 = bot2.Style;
                var settings2 = new WidgetSettingsDto
                {
                    Styles = new StyleSettings
                    {
                        LauncherBackground = style2?.PrimaryColor ?? "#000000",
                        HeaderBackground = style2?.PrimaryColor ?? "#000000",
                        HeaderText = "#FFFFFF",
                        UserMessageBackground = style2?.SecondaryColor ?? "#0084ff",
                        UserMessageText = "#FFFFFF",
                        ResponseMessageBackground = "#f4f7f9",
                        ResponseMessageText = "#000000",
                        Title = style2?.Title ?? bot2.Name ?? "Asistente Virtual",
                        Subtitle = "Powered by Voia",
                        AvatarUrl = style2?.AvatarUrl,
                        Position = style2?.Position ?? "bottom-right",
                        FontFamily = style2?.FontFamily ?? "Arial",
                        Theme = style2?.Theme ?? "light",
                        CustomCss = style2?.CustomCss,
                        HeaderBackgroundColor = style2?.HeaderBackgroundColor,
                        AllowImageUpload = style2?.AllowImageUpload ?? true,
                        AllowFileUpload = style2?.AllowFileUpload ?? true
                    },
                    WelcomeMessage = "¡Hola! ¿En qué puedo ayudarte?"
                };

                return Ok(settings2);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno al obtener la configuración del widget", Details = ex.Message });
            }
        }

        [HttpGet("{id}/context")]
        public async Task<ActionResult<object>> GetBotContext(int id, string query = "")
        {
            var bot = await _context.Bots
                .Include(b => b.User)
                .Include(b => b.Style) // ✅ FIX: Incluir la relación con BotStyle
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bot == null) return NotFound();

            var aiModelConfig = await _context.AiModelConfigs
                .Include(c => c.IaProvider)
                .FirstOrDefaultAsync(c => c.Id == bot.AiModelConfigId);

            var templatePrompts = await _context.BotTemplatePrompts
                .Where(p => p.BotTemplateId == bot.BotTemplateId)
                .ToListAsync();

            var systemPrompt = templatePrompts
                .FirstOrDefault(p => p.Role == PromptRole.system)?.Content;

            var captureFields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == bot.Id)
                .Select(f => new
                {
                    f.Id,
                    f.FieldName,
                    f.FieldType,
                    IsRequired = f.IsRequired ?? false
                })
                .ToListAsync();

            string? captureInstruction = null;
            if (captureFields.Any())
            {
                var requiredFields = string.Join(", ",
                    captureFields.Where(f => f.IsRequired).Select(f => f.FieldName));

                var optionalFields = string.Join(", ",
                    captureFields.Where(f => !f.IsRequired).Select(f => f.FieldName));

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(requiredFields))
                    parts.Add($"(obligatorios) {requiredFields}");
                if (!string.IsNullOrWhiteSpace(optionalFields))
                    parts.Add($"(opcionales) {optionalFields}");

                var fieldsPhrase = string.Join(" ", parts);

                captureInstruction =
                    $"Antes de avanzar, captura con tacto la siguiente información del usuario: {fieldsPhrase}. " +
                    "Haz preguntas de una en una, valida repitiendo el dato y pidiendo confirmación. " +
                    "Si ya tienes un dato, no lo vuelvas a pedir; continúa con el siguiente. " +
                    "Una vez confirmados los obligatorios, puedes proceder con la solicitud del usuario.";
            }

            var systemPromptFinal = string.IsNullOrWhiteSpace(systemPrompt)
                ? captureInstruction
                : $"{systemPrompt}\n\n{captureInstruction}";

            var customPrompts = await _context.BotCustomPrompts
                .Where(p => p.BotId == bot.Id)
                .OrderBy(p => p.Id)
                .ToListAsync();

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPromptFinal))
            {
                messages.Add(new { role = "system", content = systemPromptFinal });
            }
            messages.AddRange(customPrompts.Select(p => new
            {
                role = p.Role.ToLowerInvariant(),
                content = p.Content
            }));

            // Entrenamiento adicional
            var urls = await _context.TrainingUrls
                .Where(u => u.BotId == bot.Id)
                .Select(u => u.Url)
                .ToListAsync();

            var texts = await _context.TrainingCustomTexts
                .Where(t => t.BotId == bot.Id)
                .Select(t => t.Content)
                .ToListAsync();

            var documents = await _context.UploadedDocuments
                .Where(d => d.BotId == bot.Id)
                .Select(d => d.FileName)
                .ToListAsync();

            // 🔹 Traer vectores desde Python
            var vectors = await _vectorSearch.SearchVectorsAsync(bot.Id, query);

            var result = new
            {
                botId = bot.Id,
                name = bot.Name,
                description = bot.Description,
                provider = new
                {
                    id = bot.IaProviderId,
                    name = aiModelConfig?.IaProvider?.Name?.ToLower(),
                    model_config_id = aiModelConfig?.Id
                },
                settings = new
                {
                    modelName = aiModelConfig?.ModelName,
                    temperature = aiModelConfig?.Temperature ?? 0.7m,
                    frequencyPenalty = aiModelConfig?.FrequencyPenalty ?? 0.0m,
                    presencePenalty = aiModelConfig?.PresencePenalty ?? 0.0m,
                    maxTokens = 1000,
                    topP = 1.0m
                },
                messages,
                style = bot.Style, // ✅ FIX: Añadir el objeto de estilo a la respuesta
                training = new
                {
                    documents,
                    urls,
                    customTexts = texts,
                    vectors // ✅ Aquí agregamos los vectores
                },
                capture = new
                {
                    fields = captureFields.Select(f => new
                    {
                        id = f.Id,
                        name = f.FieldName,
                        type = f.FieldType,
                        required = f.IsRequired
                    })
                }
            };

            return Ok(result);
        }


    }

}
