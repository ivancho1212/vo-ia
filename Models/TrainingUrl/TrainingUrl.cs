using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("training_urls")]
    public class TrainingUrl
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; }

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("url", TypeName = "text")]
        public required string Url { get; set; }

        [Column("status")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }

        [Column("indexed")]
        public int Indexed { get; set; } = 0;

        [Column("content_hash", TypeName = "varchar(128)")]
        public string? ContentHash { get; set; }

        [Column("extracted_text", TypeName = "longtext")]
        public string? ExtractedText { get; set; }

        [Column("qdrant_id", TypeName = "varchar(128)")]
        public string? QdrantId { get; set; }

        [ForeignKey("BotId")]
        public Bot? Bot { get; set; }


    }
}
