using System.ComponentModel.DataAnnotations;

namespace Voia.Api.DTOs
{
    public class CreateSupportResponseDto
    {
        [Required]
        public int TicketId { get; set; }

        public int? ResponderId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "El mensaje no puede exceder los 1000 caracteres.")]
        public string Message { get; set; }
    }
}
