using System;

namespace Voia.Api.Models.Dtos
{
    public class TrainingUrlResponseDto
    {
        public int Id { get; set; }
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string Url { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
