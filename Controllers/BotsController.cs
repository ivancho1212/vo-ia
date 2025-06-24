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

namespace Voia.Api.Controllers
{
    // [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class BotsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotsController(ApplicationDbContext context)
        {
            _context = context;
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
                Console.WriteLine("[DEBUG] ModelState inválido.");
                return BadRequest(ModelState);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.TryParse(userIdStr, out var parsedId) ? parsedId : 45;
            Console.WriteLine($"[DEBUG] UserId autenticado: {userId}");

            var existingBot = await _context.Bots
                .FirstOrDefaultAsync(b => b.Name == botDto.Name && b.UserId == userId);

            if (existingBot != null)
            {
                Console.WriteLine("[DEBUG] Ya existe un bot con el nombre especificado.");
                return BadRequest(new { Message = "Ya existe un bot con el mismo nombre." });
            }

            var template = await _context.BotTemplates
                .Include(t => t.DefaultStyle)
                .FirstOrDefaultAsync(t => t.Id == botDto.BotTemplateId);

            if (template == null)
            {
                Console.WriteLine("[DEBUG] La plantilla especificada no existe.");
                return BadRequest(new { Message = "Plantilla no encontrada." });
            }

            if (template.IaProviderId == 0 || template.AiModelConfigId == 0)
            {
                Console.WriteLine("[DEBUG] La plantilla no tiene proveedor/modelo de IA válido.");
                return BadRequest(new { Message = "La plantilla no tiene configuración válida." });
            }

            int? reusedOrClonedStyleId = null;

            if (template.DefaultStyleId.HasValue)
            {
                var styleTemplate = await _context.StyleTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == template.DefaultStyleId.Value);

                if (styleTemplate == null)
                {
                    Console.WriteLine("[DEBUG] StyleTemplate no encontrado.");
                    return BadRequest(new { Message = "StyleTemplate no encontrado." });
                }

                string sanitizedPosition = (styleTemplate.Position ?? "bottom-right")
                    .Trim()
                    .Where(c => c >= 32 && c <= 126)
                    .Aggregate("", (acc, c) => acc + c);

                // 👉 Verificar si ya existe un estilo igual
                var existingStyle = await _context.BotStyles
                    .Where(s =>
                        s.StyleTemplateId == styleTemplate.Id &&
                        s.Theme == (styleTemplate.Theme ?? "light") &&
                        s.PrimaryColor == (styleTemplate.PrimaryColor ?? "#000000") &&
                        s.SecondaryColor == (styleTemplate.SecondaryColor ?? "#ffffff") &&
                        s.FontFamily == (styleTemplate.FontFamily ?? "Arial") &&
                        s.AvatarUrl == (styleTemplate.AvatarUrl ?? "") &&
                        s.Position == sanitizedPosition &&
                        s.CustomCss == (styleTemplate.CustomCss ?? "")
                    )
                    .FirstOrDefaultAsync();

                if (existingStyle != null)
                {
                    reusedOrClonedStyleId = existingStyle.Id;
                    Console.WriteLine($"[DEBUG] Reutilizando BotStyle existente con Id = {reusedOrClonedStyleId}.");
                }
                else
                {
                    var newBotStyle = new BotStyle
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

                    _context.BotStyles.Add(newBotStyle);
                    await _context.SaveChangesAsync();

                    reusedOrClonedStyleId = newBotStyle.Id;
                    Console.WriteLine($"[DEBUG] Clonado nuevo BotStyle con Id = {reusedOrClonedStyleId}.");
                }
            }

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

            Console.WriteLine($"[DEBUG] Bot guardado con Id = {bot.Id}.");

            var trainingSession = new BotTrainingSession
            {
                BotId = bot.Id,
                SessionName = $"Entrenamiento inicial para {botDto.Name}",
                Description = $"Sesión creada al momento de crear el bot con plantilla '{template.Name}'",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotTrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var documentosDePlantilla = await _context.UploadedDocuments
                .Where(d => d.BotTemplateId == template.Id && d.BotId == null)
                .ToListAsync();

            foreach (var doc in documentosDePlantilla)
            {
                doc.BotId = bot.Id;
                doc.TemplateTrainingSessionId = trainingSession.Id;
                Console.WriteLine($"[DEBUG] Documento ID {doc.Id} → BotId: {doc.BotId}, SessionId: {doc.TemplateTrainingSessionId}");
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(botDto.CustomText))
            {
                var trainingText = new TrainingCustomText
                {
                    BotId = bot.Id,
                    BotTemplateId = template.Id,
                    TemplateTrainingSessionId = trainingSession.Id,
                    UserId = userId,
                    Content = botDto.CustomText,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TrainingCustomTexts.Add(trainingText);
                await _context.SaveChangesAsync();
            }

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
    }
}
