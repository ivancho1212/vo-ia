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
        public BotsController(ApplicationDbContext context, FastApiService fastApiService, VectorSearchService vectorSearch)
        {
            _context = context;
            _fastApiService = fastApiService;
            _vectorSearch = vectorSearch;
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
        // [HasPermission("CanViewBots")]
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
        public async Task<ActionResult<IEnumerable<Bot>>> GetMyBots()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var bots = await _context.Bots
                    .Include(b => b.User)
                    .Where(b => b.UserId == userId && b.IsActive)
                    .ToListAsync();

                return Ok(bots);
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
        // [HasPermission("CanCreateBot")]
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
                    .FirstOrDefaultAsync(b => b.Name == botDto.Name && b.UserId == userId);

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
                    .Where(d => d.BotTemplateId == template.Id && d.BotId == null)
                    .ToListAsync();
                documentos.ForEach(d => d.BotId = bot.Id);

                var urls = await _context.TrainingUrls
                    .Where(u => u.BotTemplateId == template.Id && u.BotId == null)
                    .ToListAsync();
                urls.ForEach(u => u.BotId = bot.Id);

                var textos = await _context.TrainingCustomTexts
                    .Where(t => t.BotTemplateId == template.Id && t.BotId == null)
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
        // [HasPermission("CanUpdateBot")]
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
        //  [HasPermission("CanDeleteBot")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            bot.IsActive = false;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Bot disabled (soft deleted)" });
        }

        /// <summary>
        /// Elimina un bot y todos sus datos asociados de forma permanente (hard delete).
        /// </summary>
        /// <param name="id">ID del bot a eliminar.</param>
        /// <returns>Mensaje de confirmación.</returns>
        /// <response code="200">Bot eliminado permanentemente.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        /// <response code="500">Si ocurre un error durante la eliminación.</response>
        [HttpDelete("{id}/force")]
        public async Task<IActionResult> ForceDeleteBot(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bot = await _context.Bots.FindAsync(id);
                if (bot == null)
                {
                    // Si el bot no existe, el estado deseado (sin bot) ya se cumple.
                    return Ok(new { Message = "Bot no encontrado, se considera eliminado." });
                }

                // 1. Eliminar entidades relacionadas indirectamente (KnowledgeChunks)
                var documentIds = await _context.UploadedDocuments
                    .Where(d => d.BotId == id)
                    .Select(d => d.Id)
                    .ToListAsync();

                if (documentIds.Any())
                {
                    var knowledgeChunks = await _context.KnowledgeChunks
                        .Where(kc => documentIds.Contains(kc.UploadedDocumentId))
                        .ToListAsync();
                    if (knowledgeChunks.Any()) _context.KnowledgeChunks.RemoveRange(knowledgeChunks);
                }

                // 2. Eliminar entidades directamente relacionadas con el Bot
                var conversations = await _context.Conversations.Where(c => c.BotId == id).ToListAsync();
                if (conversations.Any()) _context.Conversations.RemoveRange(conversations);

                var trainingSessions = await _context.BotTrainingSessions.Where(ts => ts.BotId == id).ToListAsync();
                if (trainingSessions.Any()) _context.BotTrainingSessions.RemoveRange(trainingSessions);

                var customPrompts = await _context.BotCustomPrompts.Where(cp => cp.BotId == id).ToListAsync();
                if (customPrompts.Any()) _context.BotCustomPrompts.RemoveRange(customPrompts);

                var uploadedDocuments = await _context.UploadedDocuments.Where(ud => ud.BotId == id).ToListAsync();
                if (uploadedDocuments.Any()) _context.UploadedDocuments.RemoveRange(uploadedDocuments);

                var trainingUrls = await _context.TrainingUrls.Where(tu => tu.BotId == id).ToListAsync();
                if (trainingUrls.Any()) _context.TrainingUrls.RemoveRange(trainingUrls);

                var customTexts = await _context.TrainingCustomTexts.Where(ct => ct.BotId == id).ToListAsync();
                if (customTexts.Any()) _context.TrainingCustomTexts.RemoveRange(customTexts);

                // (Añadir aquí cualquier otra entidad directamente relacionada que deba ser eliminada)

                // 3. Eliminar el Bot principal
                _context.Bots.Remove(bot);

                // 4. Guardar todos los cambios en la base de datos
                await _context.SaveChangesAsync();

                // 5. Llamar a la API de FastAPI para eliminar la colección de vectores en Qdrant
                await _fastApiService.DeleteVectorCollectionAsync(id);

                // 6. Confirmar la transacción
                await transaction.CommitAsync();

                return Ok(new { Message = "Bot y todos sus datos asociados han sido eliminados permanentemente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.Error.WriteLine($"[ERROR] ForceDeleteBot para el Bot ID {id} falló: {ex.ToString()}");
                return StatusCode(500, new { Message = "Ocurrió un error interno durante la eliminación forzada del bot.", Details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un bot por su ID.
        /// </summary>
        /// <param name="id">ID del bot.</param>
        /// <returns>Bot encontrado.</returns>
        /// <response code="200">Devuelve el bot encontrado.</response>
        /// <response code="404">Si el bot no se encuentra.</response>
        [HttpGet("{id}")]
        // [HasPermission("CanViewBot")]
        public async Task<ActionResult<Bot>> GetBotById(int id)
        {
            var bot = await _context.Bots
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bot == null)
                return NotFound(new { Message = "Bot not found" });

            return Ok(bot);
        }
        [HttpGet("{id}/widget-settings")]
        [AllowAnonymous] // Permitir acceso sin autenticación
        [EnableCors("AllowWidgets")] // 🔧 CORS para widgets
        // Validación manual de token en el código, no con atributo
        public async Task<ActionResult<WidgetSettingsDto>> GetWidgetSettings(int id, [FromQuery] string token)
        {
            try
            {
                var bot = await _context.Bots
                    .Include(b => b.Style)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bot == null)
                    return NotFound(new { Message = "Bot not found" });

                // Verificar que el token corresponde a una integración válida
                var integration = await _context.BotIntegrations
                    .FirstOrDefaultAsync(bi => bi.BotId == id && bi.ApiTokenHash == token);

                if (integration == null)
                    return Unauthorized(new { Message = "Invalid token for this bot" });

                // Convertir el estilo del bot a configuración del widget (todos los campos)
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

            string captureInstruction = null;
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
                .Where(p => p.BotTemplateId == bot.BotTemplateId)
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
