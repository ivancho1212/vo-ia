namespace Voia.Api.Models.Bots
{
    public class FullBotContextDto
    {
        public int BotId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int IaProviderId { get; set; }
        public int? AiModelConfigId { get; set; } // ← Este cambio

        public string SystemPrompt { get; set; }
        public List<PromptMessageDto> CustomPrompts { get; set; }

        public List<string> Urls { get; set; }
        public List<string> CustomTexts { get; set; }
        public List<string> Documents { get; set; }
    }
}
