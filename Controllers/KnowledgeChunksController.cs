using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KnowledgeChunksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KnowledgeChunksController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los knowledge chunks.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KnowledgeChunkResponseDto>>> GetAll()
        {
            var chunks = await _context.KnowledgeChunks
                .Select(k => new KnowledgeChunkResponseDto
                {
                    Id = k.Id,
                    UploadedDocumentId = k.UploadedDocumentId,
                    Content = k.Content,
                    Metadata = k.Metadata,
                    CreatedAt = k.CreatedAt,
                    TemplateTrainingSessionId = k.TemplateTrainingSessionId
                }).ToListAsync();

            return Ok(chunks);
        }

        /// <summary>
        /// Obtiene los knowledge chunks por ID de documento.
        /// </summary>
        [HttpGet("document/{uploadedDocumentId}")]
        public async Task<ActionResult<IEnumerable<KnowledgeChunkResponseDto>>> GetByDocument(int uploadedDocumentId)
        {
            var chunks = await _context.KnowledgeChunks
                .Where(k => k.UploadedDocumentId == uploadedDocumentId)
                .Select(k => new KnowledgeChunkResponseDto
                {
                    Id = k.Id,
                    UploadedDocumentId = k.UploadedDocumentId,
                    Content = k.Content,
                    Metadata = k.Metadata,
                    CreatedAt = k.CreatedAt,
                    TemplateTrainingSessionId = k.TemplateTrainingSessionId
                }).ToListAsync();

            return Ok(chunks);
        }

        /// <summary>
        /// Obtiene un knowledge chunk por ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<KnowledgeChunkResponseDto>> GetById(int id)
        {
            var chunk = await _context.KnowledgeChunks.FindAsync(id);

            if (chunk == null)
                return NotFound();

            return new KnowledgeChunkResponseDto
            {
                Id = chunk.Id,
                UploadedDocumentId = chunk.UploadedDocumentId,
                Content = chunk.Content,
                Metadata = chunk.Metadata,
                CreatedAt = chunk.CreatedAt,
                TemplateTrainingSessionId = chunk.TemplateTrainingSessionId
            };
        }

        /// <summary>
        /// Crea un nuevo knowledge chunk.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<KnowledgeChunkResponseDto>> Create(KnowledgeChunkCreateDto dto)
        {
            var chunk = new KnowledgeChunk
            {
                UploadedDocumentId = dto.UploadedDocumentId,
                Content = dto.Content,
                Metadata = dto.Metadata,
                EmbeddingVector = dto.EmbeddingVector,
                TemplateTrainingSessionId = dto.TemplateTrainingSessionId
            };

            _context.KnowledgeChunks.Add(chunk);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = chunk.Id }, new KnowledgeChunkResponseDto
            {
                Id = chunk.Id,
                UploadedDocumentId = chunk.UploadedDocumentId,
                Content = chunk.Content,
                Metadata = chunk.Metadata,
                CreatedAt = chunk.CreatedAt,
                TemplateTrainingSessionId = chunk.TemplateTrainingSessionId
            });
        }

        /// <summary>
        /// Elimina un knowledge chunk por ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var chunk = await _context.KnowledgeChunks.FindAsync(id);

            if (chunk == null)
                return NotFound();

            _context.KnowledgeChunks.Remove(chunk);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
