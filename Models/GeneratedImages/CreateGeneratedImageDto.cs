using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.GeneratedImages
{
    public class CreateGeneratedImageDto
    {
        [Required]
        public int UserId { get; set; }

        public int? BotId { get; set; }

        [Required]
        public string Prompt { get; set; }

        [Required]
        public string ImageUrl { get; set; }
    }
}
