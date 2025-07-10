namespace Voia.Api.Models.BotConversation
{
    public class AskBotRequestDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public int? ConversationId { get; set; }

        public AskBotRequestMeta Meta { get; set; } // 👈 Agrega esta propiedad
    }

    public class AskBotRequestMeta
    {
        public bool InternalOnly { get; set; }
    }

}
