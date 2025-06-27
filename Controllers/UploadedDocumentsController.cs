using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;
using Voia.Api.Helpers;

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

        // UploadedDocumentsController.cs
        [HttpGet("by-template/{templateId}")]
        public async Task<ActionResult<IEnumerable<UploadedDocumentResponseDto>>> GetByTemplate(int templateId)
        {
            var docs = await _context.UploadedDocuments
                        .Where(x => x.BotTemplateId == templateId)
                        .ToListAsync();

            return Ok(docs);
        }

        /// <summary>
        /// Crea un nuevo documento subido.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UploadedDocumentResponseDto>> Create([FromForm] UploadedDocumentCreateDto dto)
        {
            Console.WriteLine("🟢🟢🟢🟢🟢🟢 [UPLOAD DOCUMENT] Datos recibidos en API:");
            Console.WriteLine($"- BotTemplateId: {dto.BotTemplateId}");
            Console.WriteLine($"- TemplateTrainingSessionId: {dto.TemplateTrainingSessionId}");
            Console.WriteLine($"- UserId: {dto.UserId}");
            Console.WriteLine($"- FileName: {dto.File?.FileName}");

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No se recibió un archivo válido.🟢🟢🟢🟢🟢🟢");

            var contentHash = HashHelper.ComputeFileHash(dto.File);

            var existing = await _context.UploadedDocuments
                .FirstOrDefaultAsync(d => d.ContentHash == contentHash && d.BotTemplateId == dto.BotTemplateId);

            if (existing != null)
            {
                return Conflict(new
                {
                    message = "⚠️ Este documento ya fue subido anteriormente.🟢🟢🟢🟢🟢",
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
                ContentHash = contentHash
            };

            // 🔥 LOG DE VERIFICACIÓN COMPLETO 🔥
            Console.WriteLine("🚀 [DOCUMENT DATA TO DB]");
            Console.WriteLine($"- BotTemplateId: {document.BotTemplateId}");
            Console.WriteLine($"- TemplateTrainingSessionId: {document.TemplateTrainingSessionId}");
            Console.WriteLine($"- UserId: {document.UserId}");
            Console.WriteLine($"- FileName: {document.FileName}");
            Console.WriteLine($"- FileType: {document.FileType}");
            Console.WriteLine($"- FilePath: {document.FilePath}");
            Console.WriteLine($"- UploadedAt: {document.UploadedAt}");
            Console.WriteLine($"- Indexed: {document.Indexed}");
            Console.WriteLine($"- ContentHash: {document.ContentHash}");

            _context.UploadedDocuments.Add(document);

            Console.WriteLine("💾 [SAVE TO DATABASE] Ejecutando SaveChangesAsync...");
            await _context.SaveChangesAsync();
            Console.WriteLine("✅✅✅ Documento guardado correctamente con ID: " + document.Id);

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

            _context.UploadedDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
