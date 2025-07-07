namespace Voia.Api.Models.Bots
{
    public class PromptMessageDto
    {
        public string Role { get; set; } // "user", "assistant"
        public string Content { get; set; }
    }
}
