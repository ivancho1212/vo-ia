using System;

namespace Voia.Api.Models.Dtos
{
    public class TrainingCustomTextResponseDto
    {
        public int Id { get; set; }
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
