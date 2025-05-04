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
        public string? IntegrationType { get; set; } = "widget";
        [Column("allowed_domain")]
        public string? AllowedDomain { get; set; }
        [Column("api_token")]
        public string? ApiToken { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
