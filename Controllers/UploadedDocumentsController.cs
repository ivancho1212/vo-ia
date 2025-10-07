using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;
using Voia.Api.Helpers;
using Voia.Api.Services;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadedDocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UploadedDocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los documentos subidos.
        /// </summary>
    [HttpGet]
    [HasPermission("CanViewUploadedDocuments")]
    public async Task<ActionResult<IEnumerable<UploadedDocumentResponseDto>>> GetAll()
        {
            var documents = await _context.UploadedDocuments
                .Select(d => new UploadedDocumentResponseDto
                {
                    Id = d.Id,
                    BotTemplateId = d.BotTemplateId,
                    TemplateTrainingSessionId = d.TemplateTrainingSessionId,
                    UserId = d.UserId,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    FilePath = d.FilePath,
                    UploadedAt = d.UploadedAt,
                    Indexed = d.Indexed
                }).ToListAsync();

            return Ok(documents);
        }

        /// <summary>
        /// Obtiene un documento subido por ID.
        /// </summary>
    [HttpGet("{id}")]
    [HasPermission("CanViewUploadedDocuments")]
    public async Task<ActionResult<UploadedDocumentResponseDto>> GetById(int id)
        {
            var document = await _context.UploadedDocuments.FindAsync(id);
            if (document == null) return NotFound();

            return Ok(new UploadedDocumentResponseDto
            {
                Id = document.Id,
                BotTemplateId = document.BotTemplateId,
                TemplateTrainingSessionId = document.TemplateTrainingSessionId,
                UserId = document.UserId,
                FileName = document.FileName,
                FileType = document.FileType,
                FilePath = document.FilePath,
                UploadedAt = document.UploadedAt,
                Indexed = document.Indexed
            });
        }

        /// <summary>
        /// Obtiene documentos por ID de plantilla.
        /// </summary>
    [HttpGet("by-template/{templateId}")]
    [HasPermission("CanViewUploadedDocuments")]
    public async Task<ActionResult<IEnumerable<UploadedDocumentResponseDto>>> GetByTemplate(int templateId)
        {
            var docs = await _context.UploadedDocuments
                        .Where(x => x.BotTemplateId == templateId)
                        .ToListAsync();

            return Ok(docs);
        }

        /// <summary>
        /// Crea un nuevo documento subido, extrae el texto, y lo divide en chunks.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UploadedDocumentResponseDto>> Create(
        [FromForm] UploadedDocumentCreateDto dto,
        [FromServices] TextChunkingService chunkingService,
        [FromServices] TextExtractionService textExtractionService
)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "No se recibió un archivo válido." });

            // Validar que existe el bot
            var botExists = await _context.Bots.AnyAsync(b => b.Id == dto.BotId);
            if (!botExists)
                return BadRequest($"El Bot con ID {dto.BotId} no existe.");

            // Validar que existe el template
            var templateExists = await _context.BotTemplates.AnyAsync(t => t.Id == dto.BotTemplateId);
            if (!templateExists)
                return BadRequest($"El BotTemplate con ID {dto.BotTemplateId} no existe.");

            var contentHash = HashHelper.ComputeFileHash(dto.File);

            var existing = await _context.UploadedDocuments
                .FirstOrDefaultAsync(d => d.ContentHash == contentHash && d.BotTemplateId == dto.BotTemplateId);

            if (existing != null)
            {
                return Conflict(new
                {
                    message = "⚠️ Este documento ya fue subido anteriormente.",
                    existingId = existing.Id
                });
            }

            var fileName = Path.GetFileName(dto.File.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            
            // Obtenemos la ruta base del proyecto
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            while (!System.IO.File.Exists(Path.Combine(basePath, "Voia.Api.csproj")))
            {
                basePath = Path.GetDirectoryName(basePath);
                if (string.IsNullOrEmpty(basePath)) throw new Exception("No se pudo encontrar la raíz del proyecto");
            }
            
            // Aseguramos que la carpeta existe
            var uploadsFolder = Path.Combine(basePath, "Uploads", "Documents");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Ruta física completa para guardar el archivo
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
            // Ruta relativa para guardar en la base de datos (con forward slashes)
            var filePath = $"Uploads/Documents/{uniqueFileName}";

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // 🧠 Paso 1: Extraer texto
            string extractedText;
            try
            {
                extractedText = textExtractionService.ExtractText(physicalPath, dto.File.ContentType);
            }
            catch (Exception ex)
            {
                // 🧹 Eliminar archivo si está corrupto o no legible
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return UnprocessableEntity(new
                {
                    message = "❌ El archivo no se puede procesar. Verifica que no esté cifrado o dañado.",
                    error = ex.Message
                });
            }

            // Validar si no se extrajo nada
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return UnprocessableEntity(new
                {
                    message = "❌ El archivo no contiene texto legible. Verifica que no esté vacío, cifrado o dañado."
                });
            }

            var document = new UploadedDocument
            {
                BotId = dto.BotId,
                BotTemplateId = dto.BotTemplateId,
                TemplateTrainingSessionId = dto.TemplateTrainingSessionId,
                UserId = dto.UserId,
                FileName = fileName,
                FileType = dto.File.ContentType,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                Indexed = false,
                ContentHash = contentHash,
                ExtractedText = extractedText // 👈 opcional
            };

            _context.UploadedDocuments.Add(document);
            await _context.SaveChangesAsync();

            // 🧠 Paso 2: Dividir en chunks y guardar
            var chunks = chunkingService.SplitTextIntoChunks(
                extractedText,
                "uploaded_document",
                document.Id,
                document.TemplateTrainingSessionId,
                document.Id
            );

            _context.KnowledgeChunks.AddRange(chunks);
            await _context.SaveChangesAsync();

            // 🔄 Paso 3: Llamar al servicio de vectorización
            try 
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync($"http://localhost:8000/process_documents?bot_id={document.BotId}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error al llamar al servicio de vectorización: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Documento enviado a vectorización para el bot {document.BotId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al contactar el servicio de vectorización: {ex.Message}");
                // No lanzamos la excepción para no interrumpir el flujo principal
            }

            return CreatedAtAction(nameof(GetById), new { id = document.Id }, new UploadedDocumentResponseDto
            {
                Id = document.Id,
                BotTemplateId = document.BotTemplateId,
                TemplateTrainingSessionId = document.TemplateTrainingSessionId,
                UserId = document.UserId,
                FileName = document.FileName,
                FileType = document.FileType,
                FilePath = document.FilePath,
                UploadedAt = document.UploadedAt,
                Indexed = document.Indexed
            });
        }


        /// <summary>
        /// Elimina un documento subido.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.UploadedDocuments.FindAsync(id);
            if (document == null) return NotFound();

            try
            {
                // Intentar eliminar archivo físico si existe
                if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
                {
                    System.IO.File.Delete(document.FilePath);
                }
            }
            catch (Exception ex)
            {
                // Loguear el error, pero no bloquea la eliminación de la base de datos
                Console.WriteLine($"⚠️ No se pudo eliminar el archivo físico: {ex.Message}");
            }

            try
            {
                _context.UploadedDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error eliminando registro en DB: {ex.Message}");
                return StatusCode(500, new { message = "Error eliminando registro en la base de datos." });
            }

            return NoContent();
        }

    }
}
