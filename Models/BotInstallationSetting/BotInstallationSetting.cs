using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_installation_settings")]
    public class BotInstallationSetting
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        [Required]
        public int BotId { get; set; }

        [Column("installation_method")]
        [Required]
        [MaxLength(10)]
        public string InstallationMethod { get; set; } // script, sdk, endpoint

        [Column("installation_instructions")]
        public string InstallationInstructions { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
