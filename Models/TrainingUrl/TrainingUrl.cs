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

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("url")]
        public string Url { get; set; }

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("bot_id")]
        public int? BotId { get; set; }

        [ForeignKey("BotId")]
        public Bot? Bot { get; set; }
        [Column("content_hash")]
        public string? ContentHash { get; set; }

        [Column("qdrant_id")]
        public string? QdrantId { get; set; }

        [Column("extracted_text")]
        public string? ExtractedText { get; set; }

        [Column("indexed")]
        public bool Indexed { get; set; } = false;


    }
}
