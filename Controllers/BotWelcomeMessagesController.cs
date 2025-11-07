using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Bots;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotWelcomeMessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BotWelcomeMessagesController> _logger;

        public BotWelcomeMessagesController(
            ApplicationDbContext context,
            ILogger<BotWelcomeMessagesController> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el mensaje de bienvenida personalizado basado en ubicación
        /// Estrategia inteligente de fallback - 7 NIVELES:
        /// 
        /// NIVEL 1-4: Mensajes específicos del bot (botId = X)
        /// 1️⃣ Visitante: País+Ciudad exacto
        /// 2️⃣ Visitante: Solo País
        /// 3️⃣ Admin: País+Ciudad del admin que creó el bot
        /// 4️⃣ Admin: Solo País del admin
        /// 
        /// NIVEL 5-7: Plantillas genéricas (botId IS NULL)
        /// 5️⃣ Exacto: País+Ciudad (sin bot_id)
        /// 6️⃣ País: Solo País (sin bot_id)
        /// 7️⃣ Global: Sin país ni ciudad (sin bot_id)
        /// </summary>
        /// <param name="botId">ID del bot</param>
        /// <param name="country">Código de país del visitante (ej: "ES", "CO")</param>
        /// <param name="city">Nombre de la ciudad del visitante (ej: "Madrid", "Bogotá")</param>
        /// <param name="language">Idioma (default: "es")</param>
        /// <returns>Mensaje de bienvenida personalizado</returns>
        [HttpGet("get-by-location")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByLocation(
            [FromQuery] int botId,
            [FromQuery] string country = "Unknown",
            [FromQuery] string city = "Unknown",
            [FromQuery] string language = "es"
        )
        {
            try
            {
                // Validar que el bot existe y obtener info del admin
                var bot = await _context.Bots
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.Id == botId);

                if (bot == null)
                {
                    return NotFound(new { message = "Bot no encontrado" });
                }

                // 1️⃣ Buscar por ubicación del VISITANTE: País + Ciudad exacto
                var visitorExactMatch = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == botId &&
                        m.Country == country &&
                        m.City == city &&
                        m.Language == language
                    );

                if (visitorExactMatch != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: LEVEL 1 - VISITOR EXACT ({country}/{city})"
                    );
                    return Ok(new
                    {
                        message = visitorExactMatch.Message,
                        country = visitorExactMatch.Country,
                        city = visitorExactMatch.City,
                        language = visitorExactMatch.Language,
                        matchType = "visitor_exact",
                        source = "Visitante: País + Ciudad"
                    });
                }

                // 2️⃣ Buscar por ubicación del VISITANTE: Solo País
                var visitorCountryMatch = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == botId &&
                        m.Country == country &&
                        m.City == null &&
                        m.Language == language
                    );

                if (visitorCountryMatch != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: LEVEL 2 - VISITOR COUNTRY ({country})"
                    );
                    return Ok(new
                    {
                        message = visitorCountryMatch.Message,
                        country = visitorCountryMatch.Country,
                        city = visitorCountryMatch.City,
                        language = visitorCountryMatch.Language,
                        matchType = "visitor_country",
                        source = "Visitante: País"
                    });
                }

                // 3️⃣ Buscar por ubicación del ADMIN: País + Ciudad exacto
                if (bot.User != null && !string.IsNullOrEmpty(bot.User.Country))
                {
                    var adminCountry = bot.User.Country;
                    var adminCity = bot.User.City;

                    var adminExactMatch = await _context.BotWelcomeMessages
                        .FirstOrDefaultAsync(m =>
                            m.BotId == botId &&
                            m.Country == adminCountry &&
                            m.City == adminCity &&
                            m.Language == language
                        );

                    if (adminExactMatch != null)
                    {
                        _logger.LogInformation(
                            $"Welcome message for Bot {botId}: LEVEL 3 - ADMIN EXACT ({adminCountry}/{adminCity})"
                        );
                        return Ok(new
                        {
                            message = adminExactMatch.Message,
                            country = adminExactMatch.Country,
                            city = adminExactMatch.City,
                            language = adminExactMatch.Language,
                            matchType = "admin_exact",
                            source = "Admin: País + Ciudad"
                        });
                    }

                    // 4️⃣ Buscar por ubicación del ADMIN: Solo País
                    var adminCountryMatch = await _context.BotWelcomeMessages
                        .FirstOrDefaultAsync(m =>
                            m.BotId == botId &&
                            m.Country == adminCountry &&
                            m.City == null &&
                            m.Language == language
                        );

                    if (adminCountryMatch != null)
                    {
                        _logger.LogInformation(
                            $"Welcome message for Bot {botId}: LEVEL 4 - ADMIN COUNTRY ({adminCountry})"
                        );
                        return Ok(new
                        {
                            message = adminCountryMatch.Message,
                            country = adminCountryMatch.Country,
                            city = adminCountryMatch.City,
                            language = adminCountryMatch.Language,
                            matchType = "admin_country",
                            source = "Admin: País"
                        });
                    }
                }

                // ========================================
                // PLANTILLAS GENÉRICAS (botId IS NULL)
                // ========================================

                // 5️⃣ Plantilla genérica: Exacto País + Ciudad
                var genericExactMatch = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == null &&
                        m.Country == country &&
                        m.City == city &&
                        m.Language == language
                    );

                if (genericExactMatch != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: LEVEL 5 - GENERIC EXACT ({country}/{city})"
                    );
                    return Ok(new
                    {
                        message = genericExactMatch.Message,
                        country = genericExactMatch.Country,
                        city = genericExactMatch.City,
                        language = genericExactMatch.Language,
                        matchType = "generic_exact",
                        source = "Plantilla: País + Ciudad"
                    });
                }

                // 6️⃣ Plantilla genérica: Solo País
                var genericCountryMatch = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == null &&
                        m.Country == country &&
                        m.City == null &&
                        m.Language == language
                    );

                if (genericCountryMatch != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: LEVEL 6 - GENERIC COUNTRY ({country})"
                    );
                    return Ok(new
                    {
                        message = genericCountryMatch.Message,
                        country = genericCountryMatch.Country,
                        city = genericCountryMatch.City,
                        language = genericCountryMatch.Language,
                        matchType = "generic_country",
                        source = "Plantilla: País"
                    });
                }

                // 7️⃣ Plantilla genérica: Global por idioma
                var genericGlobalMatch = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == null &&
                        m.Country == null &&
                        m.City == null &&
                        m.Language == language
                    );

                if (genericGlobalMatch != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: LEVEL 7 - GENERIC GLOBAL ({language})"
                    );
                    return Ok(new
                    {
                        message = genericGlobalMatch.Message,
                        country = genericGlobalMatch.Country,
                        city = genericGlobalMatch.City,
                        language = genericGlobalMatch.Language,
                        matchType = "generic_global",
                        source = "Plantilla: Global"
                    });
                }

                // FALLBACK FINAL: Español global
                var defaultSpanish = await _context.BotWelcomeMessages
                    .FirstOrDefaultAsync(m =>
                        m.BotId == null &&
                        m.Country == null &&
                        m.City == null &&
                        m.Language == "es"
                    );

                if (defaultSpanish != null)
                {
                    _logger.LogInformation(
                        $"Welcome message for Bot {botId}: FALLBACK - DEFAULT SPANISH"
                    );
                    return Ok(new
                    {
                        message = defaultSpanish.Message,
                        country = defaultSpanish.Country,
                        city = defaultSpanish.City,
                        language = defaultSpanish.Language,
                        matchType = "fallback_spanish",
                        source = "Fallback: Español global"
                    });
                }

                // Si no hay ningún mensaje, devolver respuesta vacía
                _logger.LogWarning($"No welcome message configured for Bot {botId}");
                return Ok(new
                {
                    message = "",
                    country = country,
                    city = city,
                    language = language,
                    matchType = "none",
                    source = "Sin mensaje configurado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo mensaje de bienvenida para bot {botId}");
                return StatusCode(500, new { error = "Error al obtener mensaje de bienvenida" });
            }
        }

        /// <summary>
        /// Obtiene todos los mensajes de bienvenida de un bot
        /// Si botId es null, obtiene todas las plantillas genéricas
        /// </summary>
        [HttpGet("bot/{botId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByBotId(int? botId = null)
        {
            try
            {
                IQueryable<BotWelcomeMessage> query = _context.BotWelcomeMessages;

                if (botId.HasValue)
                {
                    query = query.Where(m => m.BotId == botId);
                }
                else
                {
                    query = query.Where(m => m.BotId == null);
                }

                var messages = await query
                    .Select(m => new
                    {
                        m.Id,
                        m.BotId,
                        m.Message,
                        m.Country,
                        m.City,
                        m.Language,
                        m.CreatedAt
                    })
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo mensajes de bot {botId}");
                return StatusCode(500, new { error = "Error al obtener mensajes" });
            }
        }

        /// <summary>
        /// Crea un nuevo mensaje de bienvenida para un bot
        /// </summary>
        /// <param name="botId">ID del bot</param>
        /// <param name="request">Datos del mensaje</param>
        /// <returns>Mensaje creado</returns>
        [HttpPost("bot/{botId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateWelcomeMessage(
            int botId,
            [FromBody] CreateWelcomeMessageRequest request
        )
        {
            try
            {
                // Validar que el bot existe y que el usuario es propietario
                var bot = await _context.Bots.FindAsync(botId);
                if (bot == null)
                {
                    return NotFound(new { error = "Bot no encontrado" });
                }

                var welcomeMessage = new BotWelcomeMessage
                {
                    BotId = botId,
                    Message = request.Message,
                    Country = request.Country,
                    City = request.City,
                    Language = request.Language ?? "es",
                    CreatedAt = DateTime.UtcNow
                };

                _context.BotWelcomeMessages.Add(welcomeMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Welcome message created for bot {botId}: {request.Country}/{request.City}");

                return Ok(new
                {
                    id = welcomeMessage.Id,
                    message = welcomeMessage.Message,
                    country = welcomeMessage.Country,
                    city = welcomeMessage.City,
                    language = welcomeMessage.Language,
                    createdAt = welcomeMessage.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creando mensaje de bienvenida para bot {botId}");
                return StatusCode(500, new { error = "Error al crear mensaje de bienvenida" });
            }
        }

        /// <summary>
        /// Actualiza un mensaje de bienvenida existente
        /// </summary>
        /// <param name="messageId">ID del mensaje</param>
        /// <param name="request">Datos actualizados</param>
        /// <returns>Mensaje actualizado</returns>
        [HttpPut("{messageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWelcomeMessage(
            int messageId,
            [FromBody] UpdateWelcomeMessageRequest request
        )
        {
            try
            {
                var message = await _context.BotWelcomeMessages.FindAsync(messageId);
                if (message == null)
                {
                    return NotFound(new { error = "Mensaje no encontrado" });
                }

                if (!string.IsNullOrEmpty(request.Message))
                    message.Message = request.Message;

                if (!string.IsNullOrEmpty(request.Country))
                    message.Country = request.Country;

                if (!string.IsNullOrEmpty(request.City))
                    message.City = request.City;

                if (!string.IsNullOrEmpty(request.Language))
                    message.Language = request.Language;

                _context.BotWelcomeMessages.Update(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Welcome message updated: {messageId}");

                return Ok(new
                {
                    id = message.Id,
                    message = message.Message,
                    country = message.Country,
                    city = message.City,
                    language = message.Language,
                    createdAt = message.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error actualizando mensaje de bienvenida {messageId}");
                return StatusCode(500, new { error = "Error al actualizar mensaje" });
            }
        }

        /// <summary>
        /// Elimina un mensaje de bienvenida
        /// </summary>
        /// <param name="messageId">ID del mensaje</param>
        /// <returns>Confirmación de eliminación</returns>
        [HttpDelete("{messageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteWelcomeMessage(int messageId)
        {
            try
            {
                var message = await _context.BotWelcomeMessages.FindAsync(messageId);
                if (message == null)
                {
                    return NotFound(new { error = "Mensaje no encontrado" });
                }

                _context.BotWelcomeMessages.Remove(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Welcome message deleted: {messageId}");

                return Ok(new { message = "Mensaje eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error eliminando mensaje de bienvenida {messageId}");
                return StatusCode(500, new { error = "Error al eliminar mensaje" });
            }
        }

        // DTOs
        public class CreateWelcomeMessageRequest
        {
            public required string Message { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? Language { get; set; } = "es";
        }

        public class UpdateWelcomeMessageRequest
        {
            public string? Message { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? Language { get; set; }
        }
    }
}
