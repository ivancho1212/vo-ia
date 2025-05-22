using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotTemplateUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public int IaProviderId { get; set; }

        public int? DefaultStyleId { get; set; }
    }
}
