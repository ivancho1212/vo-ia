using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using Voia.Api.Models;
using Voia.Api.Models.Dtos;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VectorEmbeddingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VectorEmbeddingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los vector embeddings (sin incluir el vector en sí).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VectorEmbeddingResponseDto>>> GetAll()
        {
            var embeddings = await _context.VectorEmbeddings
                .Select(e => new VectorEmbeddingResponseDto
                {
                    Id = e.Id,
                    KnowledgeChunkId = e.KnowledgeChunkId,
                    Provider = e.Provider,
                    CreatedAt = e.CreatedAt
                }).ToListAsync();

            return Ok(embeddings);
        }

        /// <summary>
        /// Obtiene un vector embedding completo, incluyendo el vector binario.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<VectorEmbedding>> GetById(int id)
        {
            var embedding = await _context.VectorEmbeddings.FindAsync(id);
            if (embedding == null) return NotFound();

            return Ok(embedding);
        }

        /// <summary>
        /// Crea un nuevo vector embedding.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<VectorEmbeddingResponseDto>> Create(VectorEmbeddingCreateDto dto)
        {
            var embedding = new VectorEmbedding
            {
                KnowledgeChunkId = dto.KnowledgeChunkId,
                EmbeddingVector = dto.EmbeddingVector,
                Provider = dto.Provider ?? "openai",
                CreatedAt = DateTime.UtcNow
            };

            _context.VectorEmbeddings.Add(embedding);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = embedding.Id }, new VectorEmbeddingResponseDto
            {
                Id = embedding.Id,
                KnowledgeChunkId = embedding.KnowledgeChunkId,
                Provider = embedding.Provider,
                CreatedAt = embedding.CreatedAt
            });
        }

        /// <summary>
        /// Elimina un vector embedding.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var embedding = await _context.VectorEmbeddings.FindAsync(id);
            if (embedding == null) return NotFound();

            _context.VectorEmbeddings.Remove(embedding);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
