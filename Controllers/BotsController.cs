using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Claims;
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
        // [HasPermission("CanViewBots")]
        public async Task<ActionResult<IEnumerable<Bot>>> GetMyBots()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")!.Value);

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
            {
                return BadRequest(ModelState);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.TryParse(userIdStr, out var parsedId) ? parsedId : 2;
            Console.WriteLine($"[DEBUG] UserId autenticado: {userId}");

            // Validar si ya existe un bot con ese nombre
            var existingBot = await _context.Bots
                .FirstOrDefaultAsync(b => b.Name == botDto.Name && b.UserId == userId);

            if (existingBot != null)
            {
                return BadRequest(new { Message = "Ya existe un bot con el mismo nombre." });
            }

            // Validar plantilla
            var template = await _context.BotTemplates
                .Include(t => t.DefaultStyle)
                .FirstOrDefaultAsync(t => t.Id == botDto.BotTemplateId);

            if (template == null)
            {
                return BadRequest(new { Message = "Plantilla no encontrada." });
            }

            if (template.IaProviderId == 0 || template.AiModelConfigId == 0)
            {
                return BadRequest(new { Message = "La plantilla no tiene configuración de IA válida." });
            }

            // Crear o reutilizar estilo
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

            // Crear bot
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

            // Crear sesión de entrenamiento
            var trainingSession = new BotTrainingSession
            {
                BotId = bot.Id,
                SessionName = $"Entrenamiento inicial para {botDto.Name}",
                Description = $"Sesión creada automáticamente al crear el bot desde la plantilla '{template.Name}'",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotTrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            // ✅ Heredar documentos
            var documentos = await _context.UploadedDocuments
                .Where(d => d.BotTemplateId == template.Id && d.BotId == null)
                .ToListAsync();

            foreach (var doc in documentos)
            {
                doc.BotId = bot.Id;
                // doc.TemplateTrainingSessionId = null;
            }

            // ✅ Heredar URLs
            var urls = await _context.TrainingUrls
                .Where(u => u.BotTemplateId == template.Id && u.BotId == null)
                .ToListAsync();

            foreach (var url in urls)
            {
                url.BotId = bot.Id;
                // url.TemplateTrainingSessionId = null;
            }

            // ✅ Heredar textos planos
            var textos = await _context.TrainingCustomTexts
                .Where(t => t.BotTemplateId == template.Id && t.BotId == null)
                .ToListAsync();

            foreach (var texto in textos)
            {
                texto.BotId = bot.Id;
                //texto.TemplateTrainingSessionId = null;
            }

            await _context.SaveChangesAsync();

            // ✅ Agregar texto plano adicional si se envió desde el DTO
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
                await _context.SaveChangesAsync();
            }

            // ✅ Trigger a la API de vectorización (FastAPI)
            var hayDocumentos = documentos.Any() || await _context.UploadedDocuments.AnyAsync(d => d.BotId == bot.Id);
            var hayUrls = urls.Any() || await _context.TrainingUrls.AnyAsync(u => u.BotId == bot.Id);
            var hayTextos = textos.Any() || await _context.TrainingCustomTexts.AnyAsync(t => t.BotId == bot.Id);

            if (hayDocumentos)
            {
                await _fastApiService.TriggerDocumentProcessingAsync();
            }

            if (hayUrls)
            {
                await _fastApiService.TriggerUrlProcessingAsync();
            }

            if (hayTextos)
            {
                await _fastApiService.TriggerCustomTextProcessingAsync();
            }

            Console.WriteLine("[DEBUG] Bot creado exitosamente con todos los datos heredados.");

            return CreatedAtAction(nameof(GetBotById), new { id = bot.Id }, bot);
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
