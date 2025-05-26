using System.ComponentModel.DataAnnotations;

namespace Voia.Api.DTOs
{
    public class UpdateStyleTemplateDto : CreateStyleTemplateDto
    {
        [Required]
        public int Id { get; set; }
    }
}
