using System;

namespace Voia.Api.Models.Dtos
{
    public class TrainingUrlResponseDto
    {
        public int Id { get; set; }
        public int BotTemplateId { get; set; }
        public int? BotId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public required string Url { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ContentHash { get; set; }
        public string? QdrantId { get; set; }
        public string? ExtractedText { get; set; }
        public int Indexed { get; set; }
    }
}
