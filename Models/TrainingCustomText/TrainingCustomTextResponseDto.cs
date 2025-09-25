using System;

namespace Voia.Api.Models.Dtos
{
    public class TrainingCustomTextResponseDto
    {
        public int Id { get; set; }
        public required int BotId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Indexed { get; set; }
        public string? QdrantId { get; set; }
        public string? ContentHash { get; set; }
        public required string Status { get; set; }
        public int BotTemplateId { get; set; }
    }
}
