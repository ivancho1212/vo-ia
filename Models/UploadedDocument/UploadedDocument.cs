using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("uploaded_documents")]
    public class UploadedDocument
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }

        [ForeignKey("BotTemplateId")]
        public BotTemplate BotTemplate { get; set; } // ✅ Esto le dice a EF que apunte a la tabla bot_templates

        [Column("template_training_session_id")]
        public int? TemplateTrainingSessionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("file_name")]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Column("file_type")]
        [MaxLength(20)]
        public string? FileType { get; set; }

        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        [Column("indexed")]
        public bool? Indexed { get; set; } = false;
        [Column("bot_id")]
        public int? BotId { get; set; }  // Puede ser null si solo es parte de una plantilla

        [ForeignKey("BotId")]
        public Bot? Bot { get; set; }

    }
}
