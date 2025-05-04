namespace Voia.Api.Models.DTOs
{
    public class CreateBotStyleDto
    {
        public int BotId { get; set; } // ID del bot al que pertenece el estilo
        public string Theme { get; set; } // Tema del bot ('light', 'dark', 'custom')
        public string PrimaryColor { get; set; } // Color primario (hexadecimal)
        public string SecondaryColor { get; set; } // Color secundario (hexadecimal)
        public string FontFamily { get; set; } // Familia de la fuente
        public string AvatarUrl { get; set; } // URL del avatar
        public string Position { get; set; } // Posición del widget ('bottom-right', etc.)
        public string CustomCss { get; set; } // CSS personalizado para el widget
    }
}
