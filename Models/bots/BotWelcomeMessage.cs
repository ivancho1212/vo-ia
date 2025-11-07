using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.Bots
{
    [Table("bot_welcome_messages")]
    public class BotWelcomeMessage
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("language")]
        public string Language { get; set; } = "es";

        [Column("country")]
        public string? Country { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relación con Bot
        public virtual Bot Bot { get; set; }
    }
}
