using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.BotActions
{
    [Table("bot_actions")]
    public class BotAction
    {
        public int Id { get; set; }
        [Column("bot_id")]
        public int BotId { get; set; }
        [Column("trigger_phrase")]
        public string? TriggerPhrase { get; set; }
        [Column("action_type")]
        public string ActionType { get; set; } = "reply"; // Por defecto si deseas
        public string? Payload { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
