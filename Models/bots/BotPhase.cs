using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.Bots
{
    [Table("bot_phases")]
    public class BotPhase
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        public int BotId { get; set; }

        // 'training' | 'data_capture' | 'styles' | 'integration'
        [Column("phase")]
        public string Phase { get; set; } = string.Empty;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("meta", TypeName = "json")]
        public string? Meta { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
