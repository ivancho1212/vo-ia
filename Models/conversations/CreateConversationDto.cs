using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Conversations
{
    public class CreateConversationDto
    {
        public int? UserId { get; set; }
        public int BotId { get; set; }
        
        [StringLength(500, ErrorMessage = "El título debe tener máximo 500 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "El título no puede contener caracteres '<', '>', '{' o '}'")]
        public string? Title { get; set; }
        
        [StringLength(5000, ErrorMessage = "El mensaje debe tener máximo 5000 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "El mensaje no puede contener caracteres '<', '>', '{' o '}'")]
        public string? UserMessage { get; set; }
        
        [StringLength(5000, ErrorMessage = "La respuesta debe tener máximo 5000 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "La respuesta no puede contener caracteres '<', '>', '{' o '}'")]
        public string? BotResponse { get; set; }
        
        public int? PublicUserId { get; set; }
        
        /// <summary>
        /// Browser Fingerprint: Hash único del navegador/dispositivo
        /// Se usa para identificar usuarios públicos junto con IP
        /// Previene que múltiples usuarios en misma red compartan conversaciones
        /// </summary>
        public string? BrowserFingerprint { get; set; }
    }
}
