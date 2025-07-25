using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Conversations
{
    public class UpdateStatusDto
    {
        [Required(ErrorMessage = "El estado es requerido.")]
        public string Status { get; set; }
    }
}