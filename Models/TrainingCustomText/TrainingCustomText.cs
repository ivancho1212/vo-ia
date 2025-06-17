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

        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("bot_id")]
        public int? BotId { get; set; }

        [ForeignKey("BotId")]
        public Bot? Bot { get; set; }

    }
}
