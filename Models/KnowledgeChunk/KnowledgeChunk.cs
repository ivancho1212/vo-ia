using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("knowledge_chunks")]
    public class KnowledgeChunk
    {
        public int Id { get; set; }
        [Column("bot_id")]
        public int BotId { get; set; }

        public string Content { get; set; } 

        public string Metadata { get; set; } // JSON como string

        public byte[] EmbeddingVector { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
