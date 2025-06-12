using System;

namespace Voia.Api.Models.Dtos
{
    public class VectorEmbeddingResponseDto
    {
        public int Id { get; set; }
        public int KnowledgeChunkId { get; set; }
        public string? Provider { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
