namespace Voia.Api.Models.Dtos
{
    public class TrainingCustomTextCreateDto
    {
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
    }
}
