using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models
{
    public class UpdateBotDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string ModelUsed { get; set; }

        public bool IsActive { get; set; }
    }
}
