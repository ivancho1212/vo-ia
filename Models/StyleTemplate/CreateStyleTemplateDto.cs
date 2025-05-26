using System.ComponentModel.DataAnnotations;

namespace Voia.Api.DTOs
{
    public class CreateStyleTemplateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Theme { get; set; } = "light";
        public string PrimaryColor { get; set; } = "#000000";
        public string SecondaryColor { get; set; } = "#ffffff";
        public string FontFamily { get; set; } = "Arial";
        public string AvatarUrl { get; set; }
        public string Position { get; set; } = "bottom-right";
        public string CustomCss { get; set; }
    }
}
