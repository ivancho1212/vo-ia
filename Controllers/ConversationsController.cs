using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models.Conversations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Voia.Api.Hubs;
using Microsoft.AspNetCore.Cors;
using Voia.Api.Models.Messages;
using System.Linq;
using Voia.Api.Services;
using Voia.Api.Services.Chat;
using Voia.Api.Services.Interfaces;
using Voia.Api.Attributes;
using Voia.Api.Models.Users;
using Voia.Api.Models;
using Voia.Api.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;


namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Super Admin,Admin")]
    public class ConversationsController : ControllerBase
    {
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly BotDataCaptureService _captureService;
    private readonly PromptBuilderService _promptBuilder;
    private readonly IAiProviderService _aiProviderService;
    // Logger eliminado para limpieza
    private readonly IGeoLocationService _geoLocationService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _config;

        public ConversationsController(
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext,
            BotDataCaptureService captureService,
            PromptBuilderService promptBuilder,
            IAiProviderService aiProviderService,
            IGeoLocationService geoLocationService,
            IWebHostEnvironment environment,
            IConfiguration config
        )
        {
            _context = context;
            _hubContext = hubContext;
            _captureService = captureService;
            _promptBuilder = promptBuilder;
            _aiProviderService = aiProviderService;
            // Logger eliminado para limpieza
            _geoLocationService = geoLocationService;
            _environment = environment;
            _config = config;
        }

        /// <summary>
        /// Obtiene todas las conversaciones con los datos relacionados de usuario y bot.
        /// </summary>
        [HttpGet]
    [HasPermission("CanViewConversations")]
    public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations()
        {
            var conversations = await _context.Conversations
                .Include(c => c.User)
                .Include(c => c.Bot)
                .Select(c => new
                {
                    c.Id,
                    c.Status,
                    Title = c.Title ?? string.Empty,
                    UserMessage = c.UserMessage ?? string.Empty,
                    BotResponse = c.BotResponse ?? string.Empty,
                    IsWithAI = c.IsWithAI,
                    Alias = $"Sesión {c.Id}",
                    Bot = c.Bot != null ? new { c.Bot.Name } : null
                })
                .ToListAsync();

            return Ok(conversations);
        }

        [HttpPost("get-or-create")]
        [AllowAnonymous]
        [EnableCors("AllowWidgets")]
        public async Task<IActionResult> CreateOrGetConversation([FromBody] CreateConversationDto dto)
        {
            try
            {
                
                // ✅ PASO 1: VALIDAR CLIENT SECRET O JWT (CRÍTICO PARA PÚBLICOS)
                bool isValidSecret = false;
                BotApiSettings? apiSettings = null;

                // Intenta validar como ClientSecret primero
                apiSettings = await _context.BotApiSettings.FirstOrDefaultAsync(
                    bas => bas.BotId == dto.BotId && 
                           bas.ClientSecret == dto.ClientSecret &&
                           bas.IsActive);
                
                if (apiSettings != null)
                {
                    isValidSecret = true;
                }
                
                // Si no es clientSecret válido, intentar como JWT
                if (!isValidSecret && !string.IsNullOrEmpty(dto.ClientSecret) && dto.ClientSecret.Contains("."))
                {
                    try
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var jwtKey = _config["Jwt:Key"] ?? string.Empty;
                        var key = Encoding.ASCII.GetBytes(jwtKey);

                        try
                        {
                            // Intenta validar con firma (para JWTs del backend)
                            var principal = tokenHandler.ValidateToken(dto.ClientSecret, new TokenValidationParameters
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

                            var botIdClaim = principal.FindFirst("botId");
                            if (botIdClaim != null && int.TryParse(botIdClaim.Value, out int tokenBotId) && tokenBotId == dto.BotId)
                            {
                                isValidSecret = true;
                            }
                        }
                        catch (SecurityTokenException)
                        {
                            // Fallback: Lee JWT sin validar firma (para JWTs generados en frontend)
                            try
                            {
                                var jwtWithoutValidation = tokenHandler.ReadJwtToken(dto.ClientSecret);
                                var botIdClaim = jwtWithoutValidation.Claims.FirstOrDefault(c => c.Type == "botId");
                                
                                if (botIdClaim != null && int.TryParse(botIdClaim.Value, out int tokenBotId) && tokenBotId == dto.BotId)
                                {
                                    // Validar que el token no esté expirado
                                    var expClaim = jwtWithoutValidation.Claims.FirstOrDefault(c => c.Type == "exp");
                                    if (expClaim != null && long.TryParse(expClaim.Value, out long expUnix))
                                    {
                                        var expDateTime = UnixTimeStampToDateTime(expUnix);
                                        if (DateTime.UtcNow <= expDateTime)
                                        {
                                            isValidSecret = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                // En desarrollo, permitir test secrets (test-secret-bot-*)
                if (!isValidSecret && _environment.IsDevelopment() && dto.ClientSecret?.StartsWith("test-secret-") == true)
                {
                    isValidSecret = true;
                }
                
                if (!isValidSecret)
                {
                    return Unauthorized("Invalid bot or client secret");
                }

                // ✅ PASO 2: VALIDAR ORIGEN (CORS CHECK)
                var origin = Request.Headers["Origin"].ToString();
                if (apiSettings != null && !string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(apiSettings.AllowedOrigins))
                {
                    var allowedOrigins = apiSettings.AllowedOrigins
                        .Split(',')
                        .Select(o => o.Trim())
                        .ToList();
                    
                    if (!allowedOrigins.Contains(origin))
                    {
                        return Unauthorized("Origin not allowed");
                    }
                    
                }

                // ✅ PASO 3: OBTENER BOT Y VALIDAR QUE ESTÁ ACTIVO
                var bot = await _context.Bots.FirstOrDefaultAsync(b => b.Id == dto.BotId && b.IsActive);
                if (bot == null)
                {
                    return BadRequest("Bot no encontrado o inactivo.");
                }


                // Para widgets, manejar usuarios públicos
                int? effectiveUserId = null;
                int? effectivePublicUserId = null;
                
                if (!dto.UserId.HasValue || dto.UserId <= 0)
                {
                    
                    // Para usuarios del widget, buscar o crear en public_users
                    var request = HttpContext.Request;
                    var rawIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    
                    // Normalizar IPv6 loopback (::1) a 127.0.0.1
                    var ipAddress = rawIpAddress == "::1" ? "127.0.0.1" : rawIpAddress;
                    
                    var userAgent = request.Headers.UserAgent.ToString();
                    
                    
                    var publicUser = await _context.PublicUsers.FirstOrDefaultAsync(pu => 
                        pu.IpAddress == ipAddress && 
                        pu.BrowserFingerprint == dto.BrowserFingerprint &&
                        pu.BotId == dto.BotId);
                    
                    if (publicUser == null)
                    {
                        // Obtener geolocalización del IP
                        var (country, city) = await _geoLocationService.GetLocationAsync(ipAddress);
                        
                        
                        publicUser = new PublicUser
                        {
                            IpAddress = ipAddress,
                            BrowserFingerprint = dto.BrowserFingerprint,
                            UserAgent = userAgent,
                            Country = country,
                            City = city,
                            BotId = dto.BotId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.PublicUsers.Add(publicUser);
                        await _context.SaveChangesAsync();
                        
                    }
                    else
                    {
                    }
                    
                    effectivePublicUserId = publicUser.Id;
                    
                    // ✅ CORREGIDO: Usar UserId del bot (creador) en lugar de usuario aleatorio
                    effectiveUserId = bot.UserId;
                }
                else
                {
                    effectiveUserId = dto.UserId.Value;
                }

                // Buscar conversación existente - para widgets usar PublicUserId, para usuarios regulares usar UserId
                // Solo buscar si no es una nueva sesión forzada
                var conversation = !dto.ForceNewSession 
                    ? await _context.Conversations
                        .FirstOrDefaultAsync(c => 
                            (effectivePublicUserId.HasValue ? c.PublicUserId == effectivePublicUserId : c.UserId == effectiveUserId) && 
                            c.BotId == dto.BotId)
                    : null;

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        UserId = effectiveUserId.Value,
                        PublicUserId = effectivePublicUserId,
                        BotId = dto.BotId,
                        Title = effectivePublicUserId.HasValue ? "Widget Chat" : "Chat con bot",
                        CreatedAt = DateTime.UtcNow,
                        Status = "active",
                        IsWithAI = true
                        // 🔴 ExpiresAt se asignará cuando la conversación cierre (ClosedAt + 3 min)
                    };

                    _context.Conversations.Add(conversation);
                    await _context.SaveChangesAsync();

                    var conversationDto = new
                    {
                        id = conversation.Id,
                        alias = $"Sesión {conversation.Id}",
                        lastMessage = "",
                        updatedAt = conversation.UpdatedAt,
                        status = conversation.Status,
                        blocked = conversation.Blocked,
                        isWithAI = conversation.IsWithAI,
                        unreadCount = 0,
                        isWidget = effectivePublicUserId.HasValue,
                        publicUserId = effectivePublicUserId
                    };

                    // Notificar al panel de admin sobre la nueva conversación
                    // Incluimos las conversaciones creadas por widgets (public users)
                    await _hubContext.Clients.Group("admin").SendAsync("NewConversation", conversationDto);
                }

                return Ok(new { conversationId = conversation.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        public class CreateConversationDto
        {
            public int? UserId { get; set; }
            public int BotId { get; set; }
            public bool ForceNewSession { get; set; } = false;
            public string? BrowserFingerprint { get; set; }
            public string? ClientSecret { get; set; }  // ✅ NUEVO
        }

        [HttpPost("{id}/disconnect")]
        [AllowAnonymous]
        public async Task<IActionResult> UserDisconnected(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation != null)
            {
                conversation.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost("{id}/expire")]
        [AllowAnonymous]
        public async Task<IActionResult> ExpireConversation(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation != null)
            {
                conversation.Status = "expired";
                conversation.ClosedAt = DateTime.UtcNow;
                conversation.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[API] Conversación {id} marcada como expirada por inactividad en widget cerrado");
            }
            return Ok();
        }

        /// <summary>
        /// Devuelve las conversaciones asociadas a los bots de un usuario específico.
        /// </summary>
        [HttpGet("by-user/{userId}")]
    [HasPermission("CanViewConversations")]
    public async Task<IActionResult> GetConversationsByUser(int userId, int page = 1, int limit = 10)
        {
            try
            {
                var query = _context.Conversations
                    .Include(c => c.User)
                    .Include(c => c.Bot)
                    .Where(c => c.Bot.UserId == userId);

                var total = await query.CountAsync();

                var conversations = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.Id,
                        c.Status,
                        Title = c.Title ?? string.Empty,
                        UserMessage = c.UserMessage ?? string.Empty,
                        BotResponse = c.BotResponse ?? string.Empty,
                        CreatedAt = c.CreatedAt,
                        IsWithAI = c.IsWithAI,
                        Alias = $"Sesión {c.Id}",
                        Bot = c.Bot != null ? new { c.Bot.Name } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    page,
                    limit,
                    total,
                    conversations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener conversaciones.", error = ex.Message });
            }
        }

        [EnableCors("AllowWidgets")] // ✅ CORS para widgets
        [HttpGet("history/{conversationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConversationHistory(int conversationId)
        {
            try
            {
                var conversation = await _context.Conversations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                    return NotFound("Conversación no encontrada");

                // 🔴 VALIDACIÓN: Verificar si la conversación ha expirado
                if (conversation.ExpiresAt.HasValue && conversation.ExpiresAt < DateTime.UtcNow)
                {
                    return StatusCode(410, new { error = "Conversación expirada" });
                }

                // 🔴 VALIDACIÓN: Verificar si la conversación está cerrada
                if (conversation.Status == "closed")
                {
                    return StatusCode(410, new { error = "Conversación expirada" });
                }

                // --- Mapeo de Mensajes ---
                var rawMessages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId)
                    .Include(m => m.User)
                    .Include(m => m.PublicUser)
                    .Include(m => m.Bot)
                    .Include(m => m.ChatUploadedFile)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // ✅ Recopilar IDs de archivos ya vinculados a mensajes para evitar duplicados
                var linkedFileIds = rawMessages
                    .Where(m => m.FileId.HasValue)
                    .Select(m => m.FileId!.Value)
                    .ToHashSet();

                var messages = rawMessages.Select(m => new ConversationItemDto
                {
                    Id = m.Id,
                    Type = m.ChatUploadedFile != null
                        ? (m.ChatUploadedFile.FileType != null && m.ChatUploadedFile.FileType.StartsWith("image") ? "image" : "file")
                        : "message",
                    Text = m.MessageText,
                    Timestamp = m.CreatedAt,
                    FromRole = m.Sender,
                    FromId = m.Sender == "user" ? (m.UserId ?? m.PublicUserId) : (m.Sender == "admin" ? m.UserId : m.BotId),
                    FromName = m.Sender == "user"
                        ? (m.User != null ? m.User.Name : m.PublicUser != null ? $"Visitante {m.PublicUser.Id}" : "Usuario")
                        : (m.Sender == "admin" ? (m.User != null ? m.User.Name : "Admin") : (m.Bot != null ? m.Bot.Name : "Bot")),
                    FromAvatarUrl = m.Sender == "user"
                        ? (m.User != null ? (m.User.AvatarUrl ?? "") : "")
                        : (m.Sender == "admin" ? (m.User != null ? (m.User.AvatarUrl ?? "") : "") : ""),
                    ReplyToMessageId = m.ReplyToMessageId,
                    // ✅ Incluir datos del archivo con URL de API segura
                    FileUrl = m.ChatUploadedFile != null ? $"/api/files/chat/{m.ChatUploadedFile.Id}" : null,
                    FileName = m.ChatUploadedFile?.FileName,
                    FileType = m.ChatUploadedFile?.FileType
                }).ToList();

                // --- Mapeo de Archivos (solo los NO vinculados a mensajes) ---
                var files = await _context.ChatUploadedFiles
                    .AsNoTracking()
                    .Where(f => f.ConversationId == conversationId && !linkedFileIds.Contains(f.Id))
                    .Include(f => f.User)
                    .Select(f => new ConversationItemDto
                    {
                        Id = f.Id,
                        Type = f.FileType != null && f.FileType.StartsWith("image") ? "image" : "file",
                        Timestamp = f.UploadedAt ?? DateTime.UtcNow,
                        FromRole = "user",
                        FromId = f.UserId,
                        FromName = f.User != null ? f.User.Name : "Usuario",
                        FromAvatarUrl = f.User != null ? (f.User.AvatarUrl ?? "") : "",
                        FileUrl = $"/api/files/chat/{f.Id}",
                        FileName = f.FileName,
                        FileType = f.FileType
                    })
                    .ToListAsync();

                var combinedHistory = messages.Concat(files)
                    .OrderBy(item => item.Timestamp)
                    .ToList();

                var counts = rawMessages.GroupBy(m => (m.Sender ?? "user").ToLower())
                    .Select(g => new { sender = g.Key, count = g.Count() })
                    .ToList();

                var normalizedHistory = combinedHistory.Select(item => new
                {
                    id = item.Id,
                    type = item.Type ?? "message",
                    text = item.Text ?? string.Empty,
                    timestamp = item.Timestamp,
                    fromRole = (item.FromRole ?? "user").ToLower() == "admin" ? "admin" : ((item.FromRole ?? "user").ToLower() == "bot" ? "bot" : "user"),
                    fromId = item.FromId,
                    fromName = item.FromName,
                    fromAvatarUrl = item.FromAvatarUrl,
                    replyToMessageId = item.ReplyToMessageId,
                    fileUrl = item.FileUrl,
                    fileName = item.FileName,
                    fileType = item.FileType
                }).ToList();

                return Ok(new
                {
                    conversationDetails = new
                    {
                        id = conversation.Id,
                        title = conversation.Title,
                        status = conversation.Status,
                        isWithAI = conversation.IsWithAI
                    },
                    history = normalizedHistory,
                    debug = new { rawCount = rawMessages.Count, countsBySender = counts }
                });
            }
            catch (Exception ex)
            {
                // Loguear el error si tienes logger
                return StatusCode(500, new { Message = "Error interno al obtener el historial de la conversación.", Details = ex.Message });
            }
        }

        /// <summary>
        /// Devuelve mensajes paginados de una conversación.
        /// Query params: before (ISO datetime) - opcional; limit - número de elementos a devolver (por defecto 50).
        /// Retorna mensajes ordenados asc por timestamp (más antiguos primero), hasMore y nextBefore para paginar.
        /// </summary>
        [HttpGet("{conversationId}/messages")]
        [HasPermission("CanViewConversations")]
        public async Task<IActionResult> GetMessagesPaginated(int conversationId, [FromQuery] DateTime? before = null, [FromQuery] int limit = 50)
        {
            try
            {
            if (limit <= 0) limit = 50;
            if (limit > 200) limit = 200; // cap razonable

            var conversation = await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return NotFound("Conversación no encontrada");

            // NOTE: previous implementation used a DB-level Take(order by desc) which in some environments
            // returned only admin rows for the paged window (investigating root cause). To ensure correctness
            // we fall back to an in-memory pagination over the ordered set for now (safe for typical
            // conversation sizes). If performance becomes an issue, we can revisit with tuned SQL.

            // Fetch the most recent messages (projection, no Includes) to avoid join-driven exclusions
            var fetched = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId && (!before.HasValue || m.CreatedAt < before.Value))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Select(m => new {
                    m.Id,
                    m.UserId,
                    m.PublicUserId,
                    m.BotId,
                    Sender = (m.Sender ?? "user"),
                    Text = m.MessageText,
                    m.ReplyToMessageId,
                    m.CreatedAt
                })
                .ToListAsync();

            // We took the most recent 'limit' items in desc order — reverse to asc chronological
            fetched.Reverse();

            // Build selection and samples from fetched
            var selection = fetched;
            int totalWindow = await _context.Messages.AsNoTracking().CountAsync(m => m.ConversationId == conversationId && (!before.HasValue || m.CreatedAt < before.Value));
            bool hasMore = totalWindow > selection.Count;

            var allWindowFirst = fetched.Take(10).Select(m => new { id = m.Id, sender = m.Sender.ToLower(), createdAt = m.CreatedAt }).ToList<object>();
            var allWindowLast = fetched.Skip(Math.Max(0, fetched.Count - 10)).Take(10).Select(m => new { id = m.Id, sender = m.Sender.ToLower(), createdAt = m.CreatedAt }).ToList<object>();

            // ✅ OPTIMIZACIÓN: Fetch related data in BULK (3 queries instead of N queries per message)
            var userIds = selection.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).Distinct().ToList();
            var publicUserIds = selection.Where(m => m.PublicUserId.HasValue).Select(m => m.PublicUserId!.Value).Distinct().ToList();
            var botIds = selection.Where(m => m.BotId.HasValue).Select(m => m.BotId!.Value).Distinct().ToList();

            var usersMap = userIds.Any() ? await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name) : new Dictionary<int,string>();
            var publicUsersMap = publicUserIds.Any() ? await _context.PublicUsers.Where(p => publicUserIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"Visitante {p.Id}") : new Dictionary<int,string>();
            var botsMap = botIds.Any() ? await _context.Bots.Where(b => botIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name) : new Dictionary<int,string>();

            var msgs = selection.Select(m => new
            {
                id = m.Id,
                type = "message",
                text = m.Text ?? string.Empty,
                timestamp = m.CreatedAt,
                fromRole = m.Sender.ToLower() == "admin" ? "admin" : (m.Sender.ToLower() == "bot" ? "bot" : "user"),
                fromId = m.Sender.ToLower() == "user" ? (m.UserId ?? m.PublicUserId) : (m.Sender.ToLower() == "admin" ? m.UserId : m.BotId),
                fromName = m.Sender.ToLower() == "user"
                    ? (m.UserId.HasValue && usersMap.ContainsKey(m.UserId.Value) ? usersMap[m.UserId.Value] : (m.PublicUserId.HasValue && publicUsersMap.ContainsKey(m.PublicUserId.Value) ? publicUsersMap[m.PublicUserId.Value] : "Usuario"))
                    : (m.Sender.ToLower() == "admin" ? (m.UserId.HasValue && usersMap.ContainsKey(m.UserId.Value) ? usersMap[m.UserId.Value] : "Admin") : (m.BotId.HasValue && botsMap.ContainsKey(m.BotId.Value) ? botsMap[m.BotId.Value] : "Bot")),
                fromAvatarUrl = string.Empty,
                replyToMessageId = m.ReplyToMessageId
            }).ToList();

            // Recompute hasMore more reliably: check if there are messages older than the earliest returned
            if (msgs.Any())
            {
                var earliest = msgs.First().timestamp;
                try
                {
                    var existsOlder = await _context.Messages
                        .AsNoTracking()
                        .AnyAsync(m => m.ConversationId == conversationId && m.CreatedAt < earliest);
                    hasMore = existsOlder;
                }
                catch
                {
                    // If anything goes wrong, keep the previous heuristic
                }
            }

            // nextBefore: timestamp del primer elemento (más antiguo) para usar como cursor en la siguiente petición
            var nextBefore = msgs.Any() ? msgs.First().timestamp : (DateTime?)null;

            // DEBUG: compute counts for the window we just queried to help diagnose missing messages
            List<object> countsBySender = new List<object>();
            int rawCount = 0;
            var fetchedSample = new List<object>();
            try
            {
                var counts = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId && (nextBefore == null || m.CreatedAt >= nextBefore))
                    .GroupBy(m => (m.Sender ?? "user").ToLower())
                    .Select(g => new { sender = g.Key, count = g.Count() })
                    .ToListAsync();

                countsBySender = counts.Cast<object>().ToList();
                rawCount = counts.Sum(c => c.count);
            }
            catch (Exception ex)
            {
                // Error en conteo de mensajes, ignorado para limpieza
            }

            try
            {
                // Provide a small sample of the fetched window (ids, sender, createdAt) to verify what rows were read
                fetchedSample = selection.Select(m => new { id = m.Id, sender = (m.Sender ?? "user").ToLower(), createdAt = m.CreatedAt }).ToList<object>();
            }
            catch { /* ignore */ }

            return Ok(new
            {
                conversationId = conversationId,
                messages = msgs,
                hasMore,
                nextBefore,
                debug = new { totalWindow, rawCount, countsBySender, allWindowFirst, allWindowLast, fetchedSample }
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Devuelve mensajes paginados agrupados por día. Uso opcional para clientes que prefieren
        /// recibir la ventana paginada ya agrupada en el servidor.
        /// Query params: before (ISO datetime) - opcional; limit - número de elementos a devolver (por defecto 50).
        /// </summary>
        [HttpGet("{conversationId}/messages/grouped")]
    [HasPermission("CanViewConversations")]
        public async Task<IActionResult> GetMessagesPaginatedGrouped(int conversationId, [FromQuery] DateTime? before = null, [FromQuery] int limit = 50)
        {
            try
            {
                if (limit <= 0) limit = 50;
                if (limit > 200) limit = 200; // cap razonable

                var conversation = await _context.Conversations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                    return NotFound("Conversación no encontrada");

                // Build a combined window of messages + uploaded files ordered by timestamp so files are included
                // in the same paginated window as messages. We fetch recent messages and recent files (respecting
                // the `before` cursor) and then merge/trim to `limit` items.

                var fetchedMessages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId && (!before.HasValue || m.CreatedAt < before.Value))
                    .Include(m => m.ChatUploadedFile)
                    .Select(m => new {
                        Id = m.Id,
                        UserId = m.UserId,
                        PublicUserId = m.PublicUserId,
                        BotId = m.BotId,
                        Sender = (m.Sender ?? "user"),
                        Text = m.MessageText,
                        ReplyToMessageId = m.ReplyToMessageId,
                        Timestamp = m.CreatedAt,
                        // ✅ Incluir datos del archivo vinculado con URL de API segura
                        FileId = m.FileId,
                        FileUrl = m.ChatUploadedFile != null ? $"/api/files/chat/{m.ChatUploadedFile.Id}" : null,
                        FileName = m.ChatUploadedFile != null ? m.ChatUploadedFile.FileName : null,
                        FileType = m.ChatUploadedFile != null ? m.ChatUploadedFile.FileType : null
                    })
                    .ToListAsync();

                // ✅ Recopilar IDs de archivos ya vinculados a mensajes para evitar duplicados
                var linkedFileIds = fetchedMessages
                    .Where(m => m.FileId.HasValue)
                    .Select(m => m.FileId!.Value)
                    .ToHashSet();

                var fetchedFiles = await _context.ChatUploadedFiles
                    .AsNoTracking()
                    .Where(f => f.ConversationId == conversationId 
                        && !linkedFileIds.Contains(f.Id)
                        && (!before.HasValue || (f.UploadedAt.HasValue && f.UploadedAt < before.Value)))
                    .Include(f => f.User)
                    .Select(f => new {
                        Id = f.Id,
                        // file entries don't have UserId/PublicUserId/BotId in the same way; we expose the uploader as UserId
                        UserId = f.UserId,
                        PublicUserId = (int?)null,
                        BotId = (int?)null,
                        Sender = "user",
                        Text = (string?)null,
                        ReplyToMessageId = (int?)null,
                        Timestamp = f.UploadedAt ?? DateTime.UtcNow,
                        FileUrl = $"/api/files/chat/{f.Id}",
                        FileName = f.FileName,
                        FileType = f.FileType,
                        UploaderName = f.User != null ? f.User.Name : "Usuario"
                    })
                    .ToListAsync();

                // Project both sets into a common anonymous shape and merge
                var combinedWindow = fetchedMessages.Select(m => new {
                    id = m.Id,
                    type = m.FileUrl != null 
                        ? (m.FileType != null && m.FileType.StartsWith("image") ? "image" : "file") 
                        : "message",
                    text = m.Text ?? string.Empty,
                    timestamp = m.Timestamp,
                    fromRole = m.Sender.ToLower() == "admin" ? "admin" : (m.Sender.ToLower() == "bot" ? "bot" : "user"),
                    fromId = m.Sender.ToLower() == "user" ? (m.UserId ?? m.PublicUserId) : (m.Sender.ToLower() == "admin" ? m.UserId : m.BotId),
                    fromUserId = m.UserId,
                    fromPublicUserId = m.PublicUserId,
                    fromBotId = m.BotId,
                    fromName = (string?)null,
                    fromAvatarUrl = string.Empty,
                    replyToMessageId = m.ReplyToMessageId,
                    fileUrl = m.FileUrl,
                    fileName = m.FileName,
                    fileType = m.FileType
                }).Concat(
                    fetchedFiles.Select(f => new {
                        id = f.Id,
                        type = (f.FileType ?? "").StartsWith("image") ? "image" : "file",
                        text = string.Empty,
                        timestamp = f.Timestamp,
                        fromRole = "user",
                        fromId = f.UserId,
                        fromUserId = f.UserId,
                        fromPublicUserId = (int?)null,
                        fromBotId = (int?)null,
                        fromName = f.UploaderName,
                        fromAvatarUrl = string.Empty,
                        replyToMessageId = (int?)null,
                        fileUrl = f.FileUrl,
                        fileName = f.FileName,
                        fileType = (string?)f.FileType
                    })
                )
                .OrderByDescending(x => x.timestamp)
                .Take(limit)
                .ToList();

                // Reverse to chronological order (oldest first)
                combinedWindow.Reverse();

                // ✅ OPTIMIZACIÓN: Bulk fetch user/public/bot data (3 queries instead of N)
                var msgUserIds = combinedWindow.Where(x => x.fromUserId.HasValue).Select(x => x.fromUserId!.Value).Where(id => id != 0).Distinct().ToList();
                var msgPublicUserIds = combinedWindow.Where(x => x.fromPublicUserId.HasValue).Select(x => x.fromPublicUserId!.Value).Distinct().ToList();
                var msgBotIds = combinedWindow.Where(x => x.fromBotId.HasValue).Select(x => x.fromBotId!.Value).Distinct().ToList();

                var usersMap = msgUserIds.Any() ? await _context.Users.Where(u => msgUserIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name) : new Dictionary<int,string>();
                var publicUsersMap = msgPublicUserIds.Any() ? await _context.PublicUsers.Where(p => msgPublicUserIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"Visitante {p.Id}") : new Dictionary<int,string>();
                var botsMap = msgBotIds.Any() ? await _context.Bots.Where(b => msgBotIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name) : new Dictionary<int,string>();

                // Materialize final list with resolved fromName where missing
                var finalItems = combinedWindow.Select(x => new {
                    id = x.id,
                    type = x.type,
                    text = x.text ?? string.Empty,
                    timestamp = x.timestamp,
                    fromRole = x.fromRole,
                    fromId = x.fromId,
                    fromName = x.fromName ?? (x.fromUserId.HasValue && usersMap.ContainsKey(x.fromUserId.Value) ? usersMap[x.fromUserId.Value] : (x.fromPublicUserId.HasValue && publicUsersMap.ContainsKey(x.fromPublicUserId.Value) ? publicUsersMap[x.fromPublicUserId.Value] : (x.fromBotId.HasValue && botsMap.ContainsKey(x.fromBotId.Value) ? botsMap[x.fromBotId.Value] : "Usuario"))),
                    fromAvatarUrl = x.fromAvatarUrl,
                    replyToMessageId = x.replyToMessageId,
                    fileUrl = x.fileUrl,
                    fileName = x.fileName,
                    fileType = x.fileType
                }).ToList();

                // Helper to build Spanish label for a date
                Func<DateTime, string> FormatDayLabel = (DateTime dt) =>
                {
                    // Use server UTC date as reference. Labels will be Spanish.
                    var culture = new System.Globalization.CultureInfo("es-ES");
                    var now = DateTime.UtcNow.Date;
                    var target = dt.Date;
                    var diffDays = (int)(now - target).TotalDays;
                    if (diffDays == 0) return "Hoy";
                    if (diffDays == 1) return "Ayer";

                    // Start of week (Monday)
                    int offset = ((int)now.DayOfWeek + 6) % 7; // 0 = Monday
                    var startOfThisWeek = now.AddDays(-offset);
                    var startOfPrevWeek = startOfThisWeek.AddDays(-7);

                    // If it's the same week, show weekday name (capitalized)
                    if (target >= startOfThisWeek)
                    {
                        var weekday = target.ToString("dddd", culture);
                        // Capitalize first letter
                        return char.ToUpper(weekday[0]) + weekday.Substring(1);
                    }

                    // If within previous week, show short date (dd MMM)
                    if (target >= startOfPrevWeek && target < startOfThisWeek)
                    {
                        return target.ToString("dd MMM", culture);
                    }

                    // Older: include year if different
                    if (target.Year == now.Year) return target.ToString("dd MMM", culture);
                    return target.ToString("dd MMM yyyy", culture);
                }; 

                // Group by day (server-side) - produce a list of days with messages
                // Group days newest-first so clients can render the most-recent day(s) first
                // while keeping messages inside each day in chronological order.
                var grouped = finalItems.GroupBy(m => m.timestamp.Date)
                    .OrderByDescending(g => g.Key)
                    .Select(g => new {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        // label in Spanish
                        label = FormatDayLabel(g.Key),
                        messages = g.ToList()
                    }).ToList();

                // Compute cursor info similar to paginated endpoint
                bool hasMore = false;
                DateTime? nextBefore = null;
                if (finalItems.Any())
                {
                    var earliest = finalItems.First().timestamp;
                    try
                    {
                        var existsOlderMessages = await _context.Messages
                            .AsNoTracking()
                            .AnyAsync(m => m.ConversationId == conversationId && m.CreatedAt < earliest);
                        var existsOlderFiles = await _context.ChatUploadedFiles
                            .AsNoTracking()
                            .AnyAsync(f => f.ConversationId == conversationId && (f.UploadedAt.HasValue && f.UploadedAt < earliest));
                        hasMore = existsOlderMessages || existsOlderFiles;
                    }
                    catch
                    {
                    }
                    nextBefore = finalItems.First().timestamp;
                }

                return Ok(new { conversationId = conversationId, days = grouped, hasMore, nextBefore, orderedNewestFirst = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("with-last-message")]
    [HasPermission("CanViewConversations")]
    public async Task<IActionResult> GetConversationsWithLastMessage()
        {
            // ✅ OPTIMIZACIÓN: Fetch conversations con Bot incluido PRIMERO
            var conversations = await _context.Conversations
                .Include(c => c.Bot)
                .AsNoTracking()
                .ToListAsync();

            // ✅ OPTIMIZACIÓN: Fetch TODOS los messages en UNA query
            var allMessages = await _context.Messages
                .AsNoTracking()
                .Where(m => conversations.Select(c => c.Id).Contains(m.ConversationId))
                .ToListAsync();

            // ✅ OPTIMIZACIÓN: Fetch TODOS los files en UNA query
            var allFiles = await _context.ChatUploadedFiles
                .AsNoTracking()
                .Where(f => conversations.Select(c => c.Id).Contains(f.ConversationId))
                .ToListAsync();

            // ✅ IN-MEMORY: Procesar en C# (sin SQL Concat)
            var result = conversations.Select(c =>
            {
                // Obtener último evento (message o file) para esta conversación
                var lastMessage = allMessages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                var lastFile = allFiles
                    .Where(f => f.ConversationId == c.Id)
                    .OrderByDescending(f => f.UploadedAt)
                    .FirstOrDefault();

                // Determinar cuál es más reciente
                object? lastEvent = null;
                if (lastMessage != null && lastFile != null)
                {
                    lastEvent = lastMessage.CreatedAt >= (lastFile.UploadedAt ?? DateTime.MinValue) 
                        ? new { RawContent = lastMessage.MessageText, Timestamp = (DateTime?)lastMessage.CreatedAt, Type = "text" }
                        : (object)new { RawContent = lastFile.FileName, Timestamp = lastFile.UploadedAt, Type = lastFile.FileType.StartsWith("image") ? "image" : "file" };
                }
                else if (lastMessage != null)
                {
                    lastEvent = new { RawContent = lastMessage.MessageText, Timestamp = (DateTime?)lastMessage.CreatedAt, Type = "text" };
                }
                else if (lastFile != null)
                {
                    lastEvent = new { RawContent = lastFile.FileName, Timestamp = lastFile.UploadedAt, Type = lastFile.FileType.StartsWith("image") ? "image" : "file" };
                }

                // Procesar contenido si es texto
                string? finalContent = null;
                string? eventType = null;
                DateTime? eventTimestamp = null;

                if (lastEvent != null)
                {
                    var eventDict = (dynamic)lastEvent;
                    finalContent = eventDict.RawContent;
                    eventType = eventDict.Type;
                    eventTimestamp = eventDict.Timestamp;

                    if (eventType == "text" && !string.IsNullOrEmpty(finalContent) && finalContent.Trim().StartsWith("{"))
                    {
                        try
                        {
                            using (var doc = System.Text.Json.JsonDocument.Parse(finalContent))
                            {
                                if (doc.RootElement.TryGetProperty("UserQuestion", out var userQuestion))
                                {
                                    finalContent = userQuestion.GetString() ?? finalContent;
                                }
                                else if (doc.RootElement.TryGetProperty("Content", out var content))
                                {
                                    finalContent = content.GetString() ?? finalContent;
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Not a valid JSON, so we'll just use the original RawContent
                        }
                    }
                }

                return new
                {
                    c.Id,
                    c.Status,
                    Title = c.Title ?? string.Empty,
                    c.IsWithAI,
                    Alias = $"Sesión {c.Id}",
                    Bot = c.Bot != null ? new { c.Bot.Name } : null,
                    lastMessage = lastEvent == null ? null : new
                    {
                        Type = eventType,
                        Content = finalContent,
                        Timestamp = eventTimestamp ?? c.UpdatedAt
                    }
                };
            }).ToList();

            return Ok(result);
        }


        /// <summary>
        /// Actualiza el estado de una conversación específica.
        /// </summary>
        [HttpPatch("{id}/status")]
    [HasPermission("CanEditConversations")]
    public async Task<IActionResult> UpdateConversationStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(new { message = "El nuevo estado no puede ser nulo o vacío." });
            }

            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound(new { message = $"Conversación con ID {id} no encontrada." });
            }

            conversation.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Estado de la conversación {id} actualizado a '{dto.Status}'." });
        }

        /// <summary>
        /// Actualiza una conversación existente.
        /// </summary>
        [HttpPut("{id}")]
    [HasPermission("CanEditConversations")]
    public async Task<IActionResult> UpdateConversation(int id, [FromBody] Conversation dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound(new { message = $"Conversation with ID {id} not found." });
            }

            conversation.Title = dto.Title ?? conversation.Title;
            conversation.UserMessage = dto.UserMessage ?? conversation.UserMessage;
            conversation.BotResponse = dto.BotResponse ?? conversation.BotResponse;
            conversation.IsWithAI = dto.IsWithAI; // ← solo si quieres que pueda modificarse desde el frontend

            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return Ok(conversation);
        }

        /// <summary>
        /// Elimina una conversación por su ID.
        /// </summary>
        [HttpDelete("{id}")]
    [HasPermission("CanDeleteConversations")]
    public async Task<IActionResult> DeleteConversation(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
            {
                return NotFound(new { message = $"Conversation with ID {id} not found." });
            }

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Conversation with ID {id} was deleted successfully." });
        }
        public class UpdateIsWithAIDto
        {
            public bool IsWithAI { get; set; }
        }

        [HttpPatch("{id}/with-ai")]
        public async Task<IActionResult> UpdateIsWithAI(int id, [FromBody] UpdateIsWithAIDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
            {
                return NotFound(new { message = $"Conversación con ID {id} no encontrada." });
            }

            conversation.IsWithAI = dto.IsWithAI;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Campo isWithAI actualizado a {dto.IsWithAI} para la conversación {id}." });
        }

        /// <summary>
        /// Envía un mensaje en una conversación y procesa captura de datos + prompt.
        /// </summary>
        [HttpPost("{conversationId}/send")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] UserMessageDto request)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Bot)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return NotFound("Conversación no encontrada");

            // 1. Capturar datos del mensaje
            var newSubmissions = await _captureService.ProcessMessageAsync(
                conversation.BotId,
                conversation.UserId,
                conversationId.ToString(),
                request.Message,
                new List<DataField>() // Se pasa una lista vacía porque este endpoint no mantiene estado de campos.
            );

            // 2. Traer todos los campos con sus valores actuales
            var capturedFields = await _context.BotDataCaptureFields
                .Where(f => f.BotId == conversation.BotId)
                .Select(f => new DataField
                {
                    FieldName = f.FieldName,
                    // 🔹 CORRECCIÓN: Usamos el conversationId como identificador de sesión único para aislar los datos por visitante.
                    Value = _context.BotDataSubmissions.Where(s =>
                            s.BotId == conversation.BotId && s.CaptureFieldId == f.Id && s.SubmissionSessionId == conversationId.ToString())
                        .OrderByDescending(s => s.SubmittedAt) // Tomamos el más reciente para esta sesión
                        .Select(s => s.SubmissionValue)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 3. Construir el prompt y obtener respuesta del bot (mock o IA real)
            string finalPrompt = await _promptBuilder.BuildPromptFromBotContextAsync(
                conversation.BotId,
                conversation.UserId, // 👈 AÑADIDO: Pasamos el UserId de la conversación
                request.Message, // Mensaje del usuario
                capturedFields
            );

            string botResponse = await _aiProviderService.GetBotResponseAsync(
                conversation.BotId,
                conversation.UserId,
                finalPrompt, // Usamos el prompt completo que construimos
                capturedFields
            );

            // 4. Guardar mensaje en DB
            var message = new Message
            {
                ConversationId = conversationId,
                Sender = "ai",
                BotId = conversation.BotId,
                MessageText = botResponse,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 5. Enviar mensaje al frontend vía SignalR
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", new
                {
                    conversationId,
                    from = "ai",
                    text = botResponse,
                    id = message.Id.ToString(),
                    timestamp = message.CreatedAt
                });

            // 6. Retornar ok al POST original
            return Ok(new
            {
                captured = newSubmissions.NewSubmissions.Select(s => new { FieldName = s.CaptureField?.FieldName, s.SubmissionValue }),
                botResponse
            });
        }

        /// <summary>
        /// Obtiene la ubicación del usuario basada en su IP.
        /// Endpoint público para ser consumido desde el widget del cliente.
        /// Soporta header X-Forwarded-For para testing con IPs simuladas.
        /// </summary>
        [HttpGet("user-location")]
        [AllowAnonymous]
        [EnableCors("AllowWidgets")]
        public async Task<IActionResult> GetUserLocation()
        {
            try
            {
                // Intentar obtener IP de X-Forwarded-For (para testing/proxies)
                var ipAddress = "127.0.0.1";
                
                if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                {
                    // X-Forwarded-For puede tener múltiples IPs, tomar la primera
                    var ips = forwardedFor.ToString().Split(',');
                    ipAddress = ips[0].Trim();
                }
                else
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }
                
                var (country, city) = await _geoLocationService.GetLocationAsync(ipAddress);
                
                
                return Ok(new
                {
                    country = country ?? "Unknown",
                    city = city ?? "Unknown",
                    ipAddress = ipAddress
                });
            }
            catch (Exception ex)
            {
                
                return Ok(new
                {
                    country = "Unknown",
                    city = "Unknown",
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🆕 Notificar al backend que una sesión móvil se unió a una conversación
        /// </summary>
        [HttpPost("{conversationId}/join-mobile")]
        [AllowAnonymous]
        public async Task<IActionResult> JoinMobileSession(int conversationId, [FromBody] JoinMobileDto dto)
        {
            try
            {
                Console.WriteLine($"[SignalR] Emitiendo MobileSessionStarted: conversationId={conversationId}, deviceType={dto.DeviceType}, grupo={conversationId}");

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversación no encontrada" });
                }

                // Validaciones de seguridad
                if (conversation.Status == "closed")
                {
                    return StatusCode(410, new { error = "La conversación está cerrada y no puede ser reabierta." });
                }
                if (conversation.ExpiresAt.HasValue && conversation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return StatusCode(410, new { error = "La conversación ha expirado y no puede ser reabierta." });
                }
                if (conversation.ActiveMobileSession)
                {
                    return StatusCode(403, new { error = "Ya existe una sesión móvil activa en esta conversación." });
                }

                // Actualizar estado solo si pasa validaciones
                conversation.ActiveMobileSession = true;
                conversation.MobileDeviceType = dto.DeviceType ?? "mobile";
                conversation.MobileJoinedAt = dto.Timestamp ?? DateTime.UtcNow;
                conversation.LastActiveAt = DateTime.UtcNow;
                conversation.Blocked = true; // Bloquear web
                await _context.SaveChangesAsync();


                // 📢 Notificar a web via SignalR SOLO si la unión fue permitida
                await _hubContext.Clients.Group(conversationId.ToString())
                    .SendAsync("MobileSessionStarted", new
                    {
                        conversationId,
                        deviceType = dto.DeviceType,
                        joinedAt = conversation.MobileJoinedAt
                    });


                return Ok(new
                {
                    success = true,
                    message = "Sesión móvil iniciada",
                    conversationId,
                    activeAt = conversation.MobileJoinedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al iniciar sesión móvil" });
            }
        }

        /// <summary>
        /// 🆕 Notificar al backend que una sesión móvil se cerró
        /// </summary>
        [HttpPost("{conversationId}/leave-mobile")]
        [AllowAnonymous]
        public async Task<IActionResult> LeaveMobileSession(int conversationId)
        {
            try
            {
                Console.WriteLine($"[SignalR] Emitiendo MobileSessionEnded: conversationId={conversationId}, grupo={conversationId}");

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversación no encontrada" });
                }

                // Marcar como cerrada
                conversation.ActiveMobileSession = false;
                conversation.Status = "closed";
                conversation.ClosedAt = DateTime.UtcNow;
                conversation.Blocked = false; // Desbloquear web
                // 🔴 NEW: Asignar ExpiresAt = ClosedAt (expira INMEDIATAMENTE al cerrar, sin ventana de 3 minutos)
                // Esto previene que alguien pueda acceder a una conversación cerrada cambiando el ID del QR
                conversation.ExpiresAt = conversation.ClosedAt.Value;
                await _context.SaveChangesAsync();


                // 📢 Notificar a web via SignalR
                await _hubContext.Clients.Group(conversationId.ToString())
                    .SendAsync("MobileSessionEnded", new
                    {
                        conversationId,
                        reason = "user-closed",
                        closedAt = conversation.ClosedAt
                    });


                return Ok(new
                {
                    success = true,
                    message = "Sesión móvil cerrada",
                    conversationId,
                    closedAt = conversation.ClosedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al cerrar sesión móvil" });
            }
        }

        /// <summary>
        /// 🆕 Actualizar timestamp de actividad (reset inactivity timer)
        /// </summary>
        [HttpPost("{conversationId}/activity")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateActivity(int conversationId)
        {
            try
            {
                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversación no encontrada" });
                }

                conversation.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();


                return Ok(new
                {
                    success = true,
                    lastActiveAt = conversation.LastActiveAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al actualizar actividad" });
            }
        }

        /// <summary>
        /// 🆕 Obtener estado actual de la conversación (para validaciones)
        /// </summary>
        [HttpGet("{conversationId}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConversationStatus(int conversationId)
        {
            try
            {
                var conversation = await _context.Conversations
                    .Where(c => c.Id == conversationId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Status,
                        c.ActiveMobileSession,
                        c.MobileDeviceType,
                        c.Blocked,
                        c.ClosedAt,
                        c.LastActiveAt,
                        c.MobileJoinedAt
                    })
                    .FirstOrDefaultAsync();

                if (conversation == null)
                {
                    return NotFound(new { error = "Conversación no encontrada" });
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener estado de conversación" });
            }
        }

        // Helper: Convertir Unix timestamp a DateTime
        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTime;
        }

        // DTOs internos
        public class UserMessageDto
        {
            public required string Message { get; set; }
        }

        public class UpdateStatusDto
        {
            public required string Status { get; set; }
        }
    }
}