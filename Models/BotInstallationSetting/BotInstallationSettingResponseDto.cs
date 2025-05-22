using System;

namespace Voia.Api.Models.DTOs
{
    public class BotInstallationSettingResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public string InstallationMethod { get; set; }
        public string InstallationInstructions { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
