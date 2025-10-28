using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Voia.Api.Data;
using Voia.Api.Models.Chat;
using Voia.Api.Attributes;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [BotTokenAuthorize]
    public class ChatUploadedFilesController : ControllerBase
    {
            private readonly ApplicationDbContext _context;
            private readonly IConfiguration _config;
            private readonly Voia.Api.Services.Chat.IPresignedUploadService _presigned;

            public ChatUploadedFilesController(ApplicationDbContext context, IConfiguration config, Voia.Api.Services.Chat.IPresignedUploadService presigned)
            {
                _context = context;
                _config = config;
                _presigned = presigned;
            }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetFilesByConversation(int conversationId)
        {
            var files = await _context.ChatUploadedFiles
                .Where(f => f.ConversationId == conversationId)
                .ToListAsync();

            return Ok(files);
        }

    [HttpPost]
    [AllowAnonymous] // Permitir usuarios anónimos del widget
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int conversationId)
        {
            // NOTE (2025-10-28): Este endpoint acepta multipart/form-data y guarda el archivo
            // en wwwroot/uploads/chat. Flujo recomendado:
            // 1) El cliente sube por HTTP multipart a este endpoint (streaming, validación de tipo/tamaño).
            // 2) El endpoint devuelve { id, fileName, filePath }.
            // 3) El cliente notifica al Hub (SignalR) invocando SendFile con payload { fileUrl: filePath, fileName, fileType, userId }.
            //    El Hub reutilizará el registro si ya existe (evita duplicados).
            //
            // Históricamente: el cliente convertía el archivo a base64 y lo enviaba directamente por SignalR.
            // Eso genera mayor consumo de memoria y tráfico. Con este cambio el servidor procesa el stream
            // del multipart de forma eficiente.

            try
            {
                Console.WriteLine($"📁 [ChatUpload] Iniciando subida - ConversationId: {conversationId}, File: {file?.FileName}, Size: {file?.Length}");
                
                if (file == null || file.Length == 0)
                {
                    Console.WriteLine($"❌ [ChatUpload] No se recibió archivo");
                    return BadRequest(new { error = "No file uploaded" });
                }

            // Validar tipo de archivo
            var allowedTypes = new[] { 
                // Imágenes
                "image/jpeg", "image/png", "image/gif", "image/webp", "image/jpg",
                // Documentos
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
                "application/msword", // .doc
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
                "application/vnd.ms-excel", // .xls
                "application/vnd.openxmlformats-officedocument.presentationml.presentation", // .pptx
                "application/vnd.ms-powerpoint", // .ppt
                "text/plain", // .txt
                "text/csv", // .csv
                "application/json", // .json
                "application/xml", // .xml
                "text/xml" // .xml
            };
            if (!allowedTypes.Contains(file.ContentType))
            {
                return BadRequest(new { error = "Invalid file type. Allowed types: images, PDF, Word, Excel, PowerPoint, TXT, CSV, JSON, XML." });
            }

            // Obtener la conversación para identificar el usuario
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                return BadRequest(new { error = "Conversation not found" });
            }

            // Validar tamaño máximo (configurable)
            var maxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMB", 10);
            var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return BadRequest(new { error = $"File too large. Maximum allowed size is {maxSizeMb}MB." });
            }

            // Crear nombre único para el archivo
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Guardar en wwwroot/uploads/chat para que sea servible por la app
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");

            // Crear directorio si no existe
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var physicalPath = Path.Combine(uploadsDir, fileName);

            // Guardar archivo (streaming desde IFormFile)
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Crear registro en base de datos
            var fileTypeRaw = file.ContentType ?? "application/octet-stream";
            var fileTypeSafe = fileTypeRaw.Length > 50 ? fileTypeRaw.Substring(0, 50) : fileTypeRaw;

            var chatFile = new ChatUploadedFile
            {
                ConversationId = conversationId,
                FileName = file.FileName,
                FileType = fileTypeSafe,
                FilePath = $"/uploads/chat/{fileName}",
                UploadedAt = DateTime.UtcNow,
                UserId = conversation.UserId, // Usuario registrado (si aplica)
                PublicUserId = conversation.PublicUserId // Usuario público/widget (si aplica)
            };

            _context.ChatUploadedFiles.Add(chatFile);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ [ChatUpload] Archivo subido exitosamente - ID: {chatFile.Id}, Path: {chatFile.FilePath}");
            
            return Ok(new { 
                id = chatFile.Id,
                fileName = chatFile.FileName,
                filePath = chatFile.FilePath,
                uploadedAt = chatFile.UploadedAt
            });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ChatUpload] Error al subir archivo: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor al subir archivo" });
            }
        }

        /// <summary>
        /// Crear URL "presigned" local para subir directamente (PUT) el contenido.
        /// Flujo: cliente pide presigned con nombre/tipo, recibe uploadUrl; sube con PUT; luego el PUT
        /// consumirá el token y guardará el archivo y su registro.
        /// </summary>
        [HttpPost("presigned")]
        [AllowAnonymous]
        public IActionResult CreatePresigned([FromBody] Voia.Api.Models.Upload.PresignedRequestDto dto)
        {
            // Validaciones básicas
            if (dto == null || string.IsNullOrWhiteSpace(dto.FileName) || string.IsNullOrWhiteSpace(dto.FileType))
                return BadRequest(new { message = "Invalid request" });

            // Validate file type against allowed list
            var allowedTypes = new[] {
                "image/jpeg", "image/png", "image/gif", "image/webp", "image/jpg",
                "application/pdf","application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/msword","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-excel","application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "application/vnd.ms-powerpoint","text/plain","text/csv","application/json","application/xml","text/xml"
            };
            if (!allowedTypes.Contains(dto.FileType))
                return BadRequest(new { message = "Invalid file type" });

            var maxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMB", 10);

            // Create token
            var meta = new Voia.Api.Services.Chat.PresignedUploadMetadata
            {
                FileName = dto.FileName,
                FileType = dto.FileType,
                ConversationId = dto.ConversationId,
                UserId = dto.UserId
            };

            var token = _presigned.CreateToken(meta, TimeSpan.FromMinutes(15));

            var uploadUrl = $"{Request.Scheme}://{Request.Host}/api/ChatUploadedFiles/presigned-upload/{token}";

            return Ok(new { uploadUrl, token, expiresIn = 15 * 60, maxSizeMb });
        }

        /// <summary>
        /// Endpoint para recibir el PUT desde la URL presigned local.
        /// Lee el body como stream, valida tamaño y guarda el archivo.
        /// </summary>
        [HttpPut("presigned-upload/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> PutPresignedUpload(string token)
        {
            if (!_presigned.TryConsumeToken(token, out var meta) || meta == null)
                return BadRequest(new { message = "Invalid or expired token" });

            var maxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMB", 10);
            var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;

            // Guardar en wwwroot/uploads/chat
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(meta.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var physicalPath = Path.Combine(uploadsDir, fileName);

            long total = 0;
            using (var fs = new FileStream(physicalPath, FileMode.Create))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    total += read;
                    if (total > maxSizeBytes)
                    {
                        fs.Close();
                        System.IO.File.Delete(physicalPath);
                        return BadRequest(new { message = $"File too large. Max {maxSizeMb}MB" });
                    }
                    await fs.WriteAsync(buffer.AsMemory(0, read));
                }
            }

            // Crear registro en DB
            var metaTypeRaw = meta.FileType ?? "application/octet-stream";
            var metaTypeSafe = metaTypeRaw.Length > 50 ? metaTypeRaw.Substring(0, 50) : metaTypeRaw;

            var chatFile = new ChatUploadedFile
            {
                ConversationId = meta.ConversationId,
                FileName = meta.FileName,
                FileType = metaTypeSafe,
                FilePath = $"/uploads/chat/{fileName}",
                UploadedAt = DateTime.UtcNow,
                UserId = meta.UserId
            };

            _context.ChatUploadedFiles.Add(chatFile);
            await _context.SaveChangesAsync();

            return Ok(new { id = chatFile.Id, filePath = chatFile.FilePath, fileName = chatFile.FileName });
        }
    }
}