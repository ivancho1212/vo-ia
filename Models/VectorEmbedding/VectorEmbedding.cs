using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("vector_embeddings")]
    public class VectorEmbedding
    {
        [Key]
        public int Id { get; set; }

        [Column("knowledge_chunk_id")]
        public int KnowledgeChunkId { get; set; }

        [Column("embedding_vector")]
        public byte[] EmbeddingVector { get; set; }

        [Column("provider")]
        [MaxLength(50)]
        public string? Provider { get; set; } = "openai";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
