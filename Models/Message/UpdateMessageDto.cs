namespace Voia.Api.Models.Messages.DTOs
{
    public class UpdateMessageDto
    {
        public int BotId { get; set; }
        public int? UserId { get; set; }           // ahora nullable
        public int? PublicUserId { get; set; }     // nuevo campo
        public int ConversationId { get; set; }
        public string Sender { get; set; }
        public string MessageText { get; set; }
        public int? TokensUsed { get; set; }
        public string Source { get; set; }
        public int? ReplyToMessageId { get; set; }
    }
}
