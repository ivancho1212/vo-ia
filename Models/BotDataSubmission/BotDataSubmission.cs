using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_data_submissions")]
    public class BotDataSubmission
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        [Required]
        public int BotId { get; set; }

        [Column("capture_field_id")]
        [Required]
        public int CaptureFieldId { get; set; }

        [ForeignKey("CaptureFieldId")]
        public BotDataCaptureField CaptureField { get; set; }  // ✅ navegación

        [Column("submission_value")]
    public string? SubmissionValue { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("submission_session_id")]
        [MaxLength(100)]
        public string? SubmissionSessionId { get; set; }

        [Column("conversation_id")]
        public long? ConversationId { get; set; }

        [Column("capture_intent")]
        public string? CaptureIntent { get; set; }

        [Column("capture_source")]
        public string? CaptureSource { get; set; }

        [Column("metadata_json")]
        public string? MetadataJson { get; set; }
    }

}
