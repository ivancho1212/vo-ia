namespace Voia.Api.Models.Conversations
{
    public class CreateConversationDto
    {
        public int UserId { get; set; }
        public int BotId { get; set; }
        public string Title { get; set; }  // Asegúrate de que el título se incluya
        public string UserMessage { get; set; }
        public string BotResponse { get; set; }
    }
}
