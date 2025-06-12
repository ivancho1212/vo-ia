namespace Voia.Api.Models
{
    public class BotCustomPromptUpdateDto
    {
        public int BotTemplateId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
    }
}
