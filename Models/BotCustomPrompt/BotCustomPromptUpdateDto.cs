namespace Voia.Api.Models
{
    public class BotCustomPromptUpdateDto
    {
        public int? BotTemplateId { get; set; }
        public required string Role { get; set; }
        public required string Content { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
    }
}
