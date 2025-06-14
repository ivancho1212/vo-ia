using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;

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
        /// Crea un nuevo documento subido.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UploadedDocumentResponseDto>> Create(
            [FromForm] IFormFile file,
            [FromForm] int botTemplateId,
            [FromForm] int? templateTrainingSessionId,
            [FromForm] int userId // <-- Agregado aquí
        )
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió un archivo válido.");

            var uploadsFolder = Path.Combine("Uploads", "Documents");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Path.GetFileName(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new UploadedDocument
            {
                BotTemplateId = botTemplateId,
                TemplateTrainingSessionId = templateTrainingSessionId,
                UserId = userId, // <-- Ahora se usa el valor correcto
                FileName = fileName,
                FileType = file.ContentType,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                Indexed = false
            };

            _context.UploadedDocuments.Add(document);
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

            _context.UploadedDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
