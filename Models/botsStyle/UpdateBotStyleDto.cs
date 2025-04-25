namespace Voia.Api.Models.DTOs
{
    public class UpdateBotStyleDto
{
    public string Theme { get; set; }

    public string PrimaryColor { get; set; }

    public string SecondaryColor { get; set; }

    public string FontFamily { get; set; }

    public string AvatarUrl { get; set; }
    public string Position { get; set; }
    public string CustomCss { get; set; }
}

}
