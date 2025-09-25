namespace Voia.Api.Models.Dtos
{
    public class TrainingCustomTextCreateDto
    {
        public required int BotId { get; set; }
        public required int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public required int UserId { get; set; }
        public required string Content { get; set; }
    }
}
