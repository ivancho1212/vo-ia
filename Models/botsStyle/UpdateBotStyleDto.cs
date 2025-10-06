namespace Voia.Api.Models
{
    public class UpdateBotStyleDto
    {
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public int? StyleTemplateId { get; set; }
        public string Theme { get; set; } = "light"; // Default value
        public string PrimaryColor { get; set; } = "#000000"; // Default value
        public string SecondaryColor { get; set; } = "#ffffff"; // Default value
        public string FontFamily { get; set; } = "Arial"; // Default value
        public string? AvatarUrl { get; set; }
        public string Position { get; set; } = "bottom-right"; // Default value
        public string? CustomCss { get; set; }
        public string? HeaderBackgroundColor { get; set; }
        public string? Title { get; set; }
        public bool AllowImageUpload { get; set; } = true; // Default value
        public bool AllowFileUpload { get; set; } = true; // Default value
    }
}
