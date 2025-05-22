using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotInstallationSettingUpdateDto
    {
        [Required]
        public int BotId { get; set; }

        [Required]
        [RegularExpression("^(script|sdk|endpoint)$", ErrorMessage = "Método de instalación inválido.")]
        public string InstallationMethod { get; set; }

        public string InstallationInstructions { get; set; }
    }
}
