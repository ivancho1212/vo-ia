using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotDataSubmissionCreateDto
    {
        [Required]
        public int BotId { get; set; }

        [Required]
        public int CaptureFieldId { get; set; }

        public string SubmissionValue { get; set; }

        // ✅ Nuevos campos para rastrear el origen
        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? SubmissionSessionId { get; set; }
        
        // New contextual fields
        public long? ConversationId { get; set; }
        public string? CaptureIntent { get; set; }
        public string? CaptureSource { get; set; }
        public string? MetadataJson { get; set; }
    }
}
