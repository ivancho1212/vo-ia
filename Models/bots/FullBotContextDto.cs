namespace Voia.Api.Models.Bots
{
    public class FullBotContextDto
    {
        public int BotId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int IaProviderId { get; set; }
        public int? AiModelConfigId { get; set; }

        public string SystemPrompt { get; set; }
        public List<BotPromptMessageDto> CustomPrompts { get; set; }

        public List<string> Urls { get; set; }
        public List<string> CustomTexts { get; set; }
        public List<string> Documents { get; set; }

        public CaptureDto Capture { get; set; }
    }

    public class CaptureDto
    {
        public List<CaptureFieldDto> Fields { get; set; }
    }

    public class CaptureFieldDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
    }

    public class BotPromptMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
