namespace Voia.Api.Models.Dtos
{
    public class VectorEmbeddingCreateDto
    {
        public int KnowledgeChunkId { get; set; }
        public byte[] EmbeddingVector { get; set; }
        public string? Provider { get; set; }
    }
}
