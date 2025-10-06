using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.BotIntegrations
{
    [Table("bot_integrations")]
    public class BotIntegration
    {
        public int Id { get; set; }

        [Column("bot_id")]
        public int BotId { get; set; }

        [Column("integration_type")]
        public string IntegrationType { get; set; } = "widget";

        // Columna añadida para configuraciones flexibles en formato JSON.
        [Column("settings_json")]
        public string SettingsJson { get; set; }

        // Columna renombrada y tipo cambiado para almacenar el JWT completo.
        [Column("api_token_hash")]
        public string ApiTokenHash { get; set; }

        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Las propiedades antiguas se eliminan para coincidir con la nueva estructura de la BD.
        // public string? AllowedDomain { get; set; }
        // public string? ApiToken { get; set; }
    }
}