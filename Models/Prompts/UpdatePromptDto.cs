namespace Voia.Api.Models.Prompts.DTOs
{
    public class UpdatePromptDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public int? ConversationId { get; set; }
        public string PromptText { get; set; }
        public string? ResponseText { get; set; }
        public int? TokensUsed { get; set; }
        public string Source { get; set; }
    }
}
