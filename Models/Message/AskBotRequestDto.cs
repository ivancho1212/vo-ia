namespace Voia.Api.Models.Conversations
{
    public class AskBotRequestDto
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public int? ReplyToMessageId { get; set; } // 👈 Esto permite vincular respuestas
        public string? TempId { get; set; }        // 👈 ID temporal del cliente
    }
}
