using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.GeneratedImages
{
    public class UpdateGeneratedImageDto
    {
        [Required]
        public string Prompt { get; set; }

        [Required]
        public string ImageUrl { get; set; }
    }
}
