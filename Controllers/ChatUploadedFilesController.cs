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

        public ChatUploadedFilesController(ApplicationDbContext context)
        {
            _context = context;
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

            // Crear nombre único para el archivo
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            
            // Crear directorio si no existe
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }
            
            var filePath = Path.Combine(uploadsDir, fileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Crear registro en base de datos
            var chatFile = new ChatUploadedFile
            {
                ConversationId = conversationId,
                FileName = file.FileName,
                FileType = file.ContentType,
                FilePath = $"/uploads/{fileName}",
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
    }
}