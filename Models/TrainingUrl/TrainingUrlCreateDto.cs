namespace Voia.Api.Models.Dtos
{
    public class TrainingUrlCreateDto
    {
        public int BotId { get; set; }
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public required string Url { get; set; }
    }
}
