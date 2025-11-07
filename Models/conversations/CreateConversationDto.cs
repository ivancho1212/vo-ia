namespace Voia.Api.Models.Conversations
{
    public class CreateConversationDto
    {
        public int? UserId { get; set; }
        public int BotId { get; set; }
        public string Title { get; set; }  // Asegúrate de que el título se incluya
        public string UserMessage { get; set; }
        public string BotResponse { get; set; }
        public int? PublicUserId { get; set; }
        
        /// <summary>
        /// Browser Fingerprint: Hash único del navegador/dispositivo
        /// Se usa para identificar usuarios públicos junto con IP
        /// Previene que múltiples usuarios en misma red compartan conversaciones
        /// </summary>
        public string? BrowserFingerprint { get; set; }

    }
}
