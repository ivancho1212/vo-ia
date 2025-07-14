namespace Voia.Api.Models.Messages.DTOs
{
    public class CreateMessageDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public int ConversationId { get; set; }
        public string Sender { get; set; } = "user";
        public string MessageText { get; set; }
        public int? TokensUsed { get; set; }
        public string Source { get; set; }

        // ✅ Agregar esto:
        public int? ReplyToMessageId { get; set; }
    }
}
