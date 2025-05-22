namespace Voia.Api.Models.DTOs
{
    public class BotTemplatePromptCreateDto
    {
        public int BotTemplateId { get; set; }
        public string Role { get; set; } = "system";
        public string Content { get; set; }
    }
}
