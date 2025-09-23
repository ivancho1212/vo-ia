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

            var uploadsFolder = Path.Combine("Uploads", "Documents");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Path.GetFileName(dto.File.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // 🧠 Paso 1: Extraer texto
            string extractedText;
            try
            {
                extractedText = textExtractionService.ExtractText(filePath, dto.File.ContentType);
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
