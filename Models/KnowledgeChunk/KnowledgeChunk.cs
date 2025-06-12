using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("knowledge_chunks")]
    public class KnowledgeChunk
    {
        public int Id { get; set; }

        [Column("uploaded_document_id")]
        public int UploadedDocumentId { get; set; }

        public UploadedDocument UploadedDocument { get; set; }

        public string Content { get; set; }

        public string Metadata { get; set; } // JSON como string

        [Column("embedding_vector")]
        public byte[] EmbeddingVector { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        public TemplateTrainingSession TemplateTrainingSession { get; set; }
    }
}
