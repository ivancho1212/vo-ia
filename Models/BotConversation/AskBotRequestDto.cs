namespace Voia.Api.Models.BotConversation
{
    public class AskBotRequestDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public int? ConversationId { get; set; } // 🆕 nuevo

    }
}
