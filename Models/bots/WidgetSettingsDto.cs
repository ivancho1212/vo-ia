using System.Text.Json.Serialization;

namespace Voia.Api.Models.Bots
{
    public class WidgetSettingsDto
    {
        public StyleSettings Styles { get; set; }
        public string WelcomeMessage { get; set; }
    }

    public class StyleSettings
    {
        [JsonPropertyName("launcherBackground")]
        public string LauncherBackground { get; set; } = "#000000";

        [JsonPropertyName("headerBackground")]
        public string HeaderBackground { get; set; } = "#000000";

        [JsonPropertyName("headerText")]
        public string HeaderText { get; set; } = "#FFFFFF";

        [JsonPropertyName("userMessageBackground")]
        public string UserMessageBackground { get; set; } = "#0084ff";

        [JsonPropertyName("userMessageText")]
        public string UserMessageText { get; set; } = "#FFFFFF";

        [JsonPropertyName("responseMessageBackground")]
        public string ResponseMessageBackground { get; set; } = "#f4f7f9";

        [JsonPropertyName("responseMessageText")]
        public string ResponseMessageText { get; set; } = "#000000";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "Chat Bot";

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; } = "Powered by Voia";

        // Campos adicionales
    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; } = "bottom-right";

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; } = "Arial";

    [JsonPropertyName("theme")]
    public string? Theme { get; set; } = "light";

    [JsonPropertyName("customCss")]
    public string? CustomCss { get; set; }

    [JsonPropertyName("headerBackgroundColor")]
    public string? HeaderBackgroundColor { get; set; }

    [JsonPropertyName("allowImageUpload")]
    public bool AllowImageUpload { get; set; } = true;

    [JsonPropertyName("allowFileUpload")]
    public bool AllowFileUpload { get; set; } = true;
    }
}