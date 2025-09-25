using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("training_custom_texts")]
    public class TrainingCustomText
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        public int BotId { get; set; }

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("content", TypeName = "text")]
        public required string Content { get; set; }

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }

        [Column("indexed")]
        public int Indexed { get; set; } = 0;

        [Column("qdrant_id", TypeName = "varchar(255)")]
        public string? QdrantId { get; set; }

        [Column("content_hash", TypeName = "varchar(255)")]
        public string? ContentHash { get; set; }

        [Column("status", TypeName = "varchar(50)")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Status { get; set; } = "pending";

        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }

        [ForeignKey("BotId")]
        public Bot? Bot { get; set; }



    }
}
