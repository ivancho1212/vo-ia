namespace Voia.Api.Models.Bots
{
    public class GeneratePromptRequestDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string UserMessage { get; set; }
    }
}
