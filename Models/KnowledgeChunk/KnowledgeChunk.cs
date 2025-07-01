using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("knowledge_chunks")]
    public class KnowledgeChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("uploaded_document_id")]
        public int UploadedDocumentId { get; set; }

        [ForeignKey("UploadedDocumentId")]
        public UploadedDocument UploadedDocument { get; set; }

        [Required]
        public string Content { get; set; }

        public string Metadata { get; set; } // JSON opcional

        [Column("embedding_vector")]
        public byte[] EmbeddingVector { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [ForeignKey("TemplateTrainingSessionId")]
        public TemplateTrainingSession TemplateTrainingSession { get; set; }
    }
}
