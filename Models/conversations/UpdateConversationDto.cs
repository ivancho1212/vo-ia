using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Conversations
{
    public class UpdateConversationDto
    {
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "El título debe tener entre 1 y 200 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "El título no puede contener caracteres peligrosos: < > { }")]
        public string Title { get; set; }

        [Required(ErrorMessage = "El mensaje del usuario es requerido")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 5000 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "El mensaje no puede contener caracteres peligrosos: < > { }")]
        public string UserMessage { get; set; }

        [Required(ErrorMessage = "La respuesta del bot es requerida")]
        [StringLength(10000, MinimumLength = 1, ErrorMessage = "La respuesta debe tener entre 1 y 10000 caracteres")]
        [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "La respuesta no puede contener caracteres peligrosos: < > { }")]
        public string BotResponse { get; set; }
    }
}
