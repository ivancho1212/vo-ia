namespace Voia.Api.Models.Dtos
{
    public class TrainingUrlCreateDto
    {
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string Url { get; set; }
    }
}
