public class CreateBotStyleDto
{
    public int? UserId { get; set; }
    public string? Name { get; set; } // ← Nuevo
    public int? StyleTemplateId { get; set; }
    public string Theme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#ffffff";
    public string FontFamily { get; set; } = "Arial";
    public string AvatarUrl { get; set; }
    public string Position { get; set; } = "bottom-right";
    public string CustomCss { get; set; }
    public string HeaderBackgroundColor { get; set; } = "#f5f5f5";
    public string? Title { get; set; }
    public bool AllowImageUpload { get; set; } = true;
    public bool AllowFileUpload { get; set; } = true;


}
