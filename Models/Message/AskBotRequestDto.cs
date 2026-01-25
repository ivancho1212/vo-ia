using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Conversations
{
    public class AskBotRequestDto
    {
        public int BotId { get; set; }
        public int? UserId { get; set; }
        
        [Required(ErrorMessage = "La pregunta es requerida")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "La pregunta debe tener entre 1 y 5000 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "El mensaje no puede contener caracteres '<', '>', '{' o '}'")]
        public string Question { get; set; } = string.Empty;
        
        public int? ReplyToMessageId { get; set; }
        public string? TempId { get; set; }
        
        // 🌍 Ubicación del usuario para contextualizar respuestas
        public UserLocationDto? UserLocation { get; set; }
    }
    
    public class UserLocationDto
    {
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Language { get; set; }
    }
}
