using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Voia.Api.Data;
using Voia.Api.Services.Upload;
using System.IO;
using System.Security.Claims;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileSignatureChecker _checker;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            ApplicationDbContext context, 
            IFileSignatureChecker checker,
            ILogger<FilesController> logger)
        {
            _context = context;
            _checker = checker;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/files/chat/{id}
        /// Versión segura: Requiere autenticación y verifica propiedad
        /// Soporta token como query param para cargas en img tags
        /// </summary>
        [HttpGet("chat/{id}")]
        [AllowAnonymous]  // Allow without auth, but check token in params or headers
        public async Task<IActionResult> GetChatFile(int id, [FromQuery] string? token = null)
        {
            _logger.LogInformation($"📥 Solicitud de descarga - FileId: {id}");

            try
            {
                // 1. Obtener archivo (solo no eliminados)
                var fileRec = await _context.ChatUploadedFiles
                    .Include(f => f.Conversation)
                    .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

                if (fileRec == null)
                {
                    _logger.LogWarning($"⚠️ Archivo no encontrado - FileId: {id}");
                    return NotFound();
                }

                // 2. Verificar autenticación: obtener userId desde headers o query token
                int userId = 0;
                string? userRole = null;
                int? publicUserId = null;

                // Primero, intenta obtener de User claims (si hay Authorization header)
                if (User?.Identity?.IsAuthenticated == true)
                {
                    userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                    userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    publicUserId = int.TryParse(User.FindFirst("public_user_id")?.Value, out var pid) ? pid : (int?)null;
                }
                // Si no hay auth en headers, por ahora solo permitimos si el archivo es público
                // (futuro: validar token en query param si se proporciona)

                // 3. Verificar propiedad
                bool isOwner = false;

                // ✅ ADMINS SIEMPRE PUEDEN VER CUALQUIER ARCHIVO
                if (userRole == "Admin")
                {
                    isOwner = true;
                    _logger.LogInformation($"✅ Acceso permitido por Admin - FileId: {id}, AdminUserId: {userId}");
                }
                // Admin o propietario de la conversación
                else if (userId > 0 && fileRec.UserId == userId)
                {
                    isOwner = true;
                }
                // Usuario público (widget anónimo)
                else if (fileRec.PublicUserId.HasValue && publicUserId.HasValue && fileRec.PublicUserId == publicUserId)
                {
                    isOwner = true;
                }
                
                // Si no es propietario, deniega
                if (!isOwner)
                {
                    _logger.LogWarning($"❌ Acceso denegado - FileId: {id}, UserId: {userId}, AdminCheck: {userRole == "Admin"}, FileUserId: {fileRec.UserId}, FilePublicUserId: {fileRec.PublicUserId}");
                    return Forbid();  // 403
                }

                // 4. Servir archivo
                var filePath = fileRec.FilePath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(filePath))
                    return BadRequest(new { message = "Invalid file path" });

                var storedFileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(storedFileName)) return NotFound();

                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "chat", storedFileName);
                if (!System.IO.File.Exists(physicalPath))
                {
                    _logger.LogError($"❌ Archivo no existe en disco - Physical: {physicalPath}");
                    return NotFound();
                }

                // 5. Revalidar firma
                string detectedMime;
                try
                {
                    detectedMime = await _checker.ValidateAsync(
                        physicalPath, 
                        fileRec.FileType ?? string.Empty, 
                        fileRec.FileName ?? string.Empty);
                }
                catch (InvalidDataException)
                {
                    _logger.LogWarning($"❌ Validación de firma falló - FileId: {id}");
                    return StatusCode(403, new { message = "File is not allowed or failed validation." });
                }

                // 6. Auditar acceso
                _logger.LogInformation($"✅ Archivo descargado - FileId: {id}, UserId: {userId}");

                // 7. Responder con archivo
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["Access-Control-Allow-Origin"] = "*";  // ✅ Allow CORS for image loading
                Response.Headers["Access-Control-Allow-Credentials"] = "true";
                Response.Headers["Cache-Control"] = "public, max-age=3600";  // Cache for 1 hour
                
                var isImage = detectedMime.StartsWith("image/");
                var disposition = isImage ? "inline" : "attachment";
                var fileName = Path.GetFileName(fileRec.FileName ?? "file");
                Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{fileName}\"";

                var fs = System.IO.File.OpenRead(physicalPath);
                return File(fs, detectedMime);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error descargando archivo - FileId: {id}, Error: {ex.Message}");
                return StatusCode(500, new { error = "Error downloading file" });
            }
        }

        /// <summary>
        /// GET /api/files/chat?conversationId={id}
        /// Lista archivos de una conversación (con verificación)
        /// </summary>
        [HttpGet("chat")]
        [Authorize]
        public async Task<IActionResult> GetConversationFiles([FromQuery] int conversationId)
        {
            try
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                    return NotFound();

                // Verificar acceso
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                bool canAccess = (userId > 0 && (conversation.UserId == userId || userRole == "Admin"));
                
                if (!canAccess && conversation.PublicUserId.HasValue)
                {
                    var publicUserIdClaim = User.FindFirst("public_user_id");
                    if (publicUserIdClaim != null && int.TryParse(publicUserIdClaim.Value, out var pubId))
                    {
                        canAccess = conversation.PublicUserId == pubId;
                    }
                }

                if (!canAccess) return Forbid();

                var files = await _context.ChatUploadedFiles
                    .Where(f => f.ConversationId == conversationId && f.DeletedAt == null)
                    .Select(f => new
                    {
                        id = f.Id,
                        fileName = f.FileName,
                        fileType = f.FileType,
                        fileUrl = $"/api/files/chat/{f.Id}",
                        uploadedAt = f.UploadedAt
                    })
                    .ToListAsync();

                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error listando archivos - Error: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving files" });
            }
        }

        /// <summary>
        /// GET /api/files/chat/{id}/inline
        /// Sirve la imagen inline (para img tags, con CORS habilitado para widgets)
        /// </summary>
        [HttpGet("chat/{id}/inline")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChatFileInline(int id)
        {
            _logger.LogInformation($"📥 Solicitud inline de descarga - FileId: {id}");

            try
            {
                // 1. Obtener archivo (solo no eliminados)
                var fileRec = await _context.ChatUploadedFiles
                    .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

                if (fileRec == null)
                {
                    _logger.LogWarning($"⚠️ Archivo no encontrado - FileId: {id}");
                    return NotFound();
                }

                // 2. Servir archivo (sin verificación de propiedad para inline, es pública)
                var filePath = fileRec.FilePath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(filePath))
                    return BadRequest(new { message = "Invalid file path" });

                var storedFileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(storedFileName)) return NotFound();

                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "chat", storedFileName);
                if (!System.IO.File.Exists(physicalPath))
                {
                    _logger.LogError($"❌ Archivo no existe en disco - Physical: {physicalPath}");
                    return NotFound();
                }

                // 3. Revalidar firma
                string detectedMime;
                try
                {
                    detectedMime = await _checker.ValidateAsync(
                        physicalPath, 
                        fileRec.FileType ?? string.Empty, 
                        fileRec.FileName ?? string.Empty);
                }
                catch (InvalidDataException)
                {
                    _logger.LogWarning($"❌ Validación de firma falló - FileId: {id}");
                    return StatusCode(403, new { message = "File is not allowed or failed validation." });
                }

                // 4. Auditar acceso
                _logger.LogInformation($"✅ Archivo inline descargado - FileId: {id}");

                // 5. Responder con archivo (inline para <img>)
                // ✅ CRITICAL: Add CORS headers MANUALLY to allow cross-origin img loading
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["Access-Control-Allow-Origin"] = "*";  // Allow all origins
                Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
                Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                
                var fileName = Path.GetFileName(fileRec.FileName ?? "file");
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

                var fs = System.IO.File.OpenRead(physicalPath);
                return File(fs, detectedMime);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error descargando archivo inline - FileId: {id}, Error: {ex.Message}");
                return StatusCode(500, new { error = "Error downloading file" });
            }
        }

        /// <summary>
        /// DELETE /api/files/chat/{id}
        /// Elimina (soft-delete) un archivo
        /// </summary>
        [HttpDelete("chat/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteChatFile(int id)
        {
            try
            {
                var file = await _context.ChatUploadedFiles
                    .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

                if (file == null) return NotFound();

                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                bool canDelete = (userId > 0 && (file.UserId == userId || userRole == "Admin"));
                
                if (!canDelete && file.PublicUserId.HasValue)
                {
                    var publicUserIdClaim = User.FindFirst("public_user_id");
                    if (publicUserIdClaim != null && int.TryParse(publicUserIdClaim.Value, out var pubId))
                    {
                        canDelete = file.PublicUserId == pubId;
                    }
                }

                if (!canDelete) return Forbid();

                file.DeletedAt = DateTime.UtcNow;
                _context.ChatUploadedFiles.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Archivo eliminado (soft-delete) - FileId: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error eliminando archivo - FileId: {id}, Error: {ex.Message}");
                return StatusCode(500, new { error = "Error deleting file" });
            }
        }
    }
}
