using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotIaProviderCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string ApiEndpoint { get; set; }

        [MaxLength(255)]
        public string ApiKey { get; set; }
    }
}
