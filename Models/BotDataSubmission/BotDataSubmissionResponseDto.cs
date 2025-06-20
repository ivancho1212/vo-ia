using System;

namespace Voia.Api.Models.DTOs
{
    public class BotDataSubmissionResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public int CaptureFieldId { get; set; }
        public string SubmissionValue { get; set; }
        public int? UserId { get; set; }
        public string? SubmissionSessionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
