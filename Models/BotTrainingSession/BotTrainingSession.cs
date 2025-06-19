using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // <- IMPORTANTE

namespace Voia.Api.Models.BotTrainingSession
{
    [Table("bot_training_sessions")] // <- ¡esto es clave!
    public class BotTrainingSession
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("bot_id")]
        public int BotId { get; set; }
        [JsonIgnore]
        public Bot Bot { get; set; }

        [Column("session_name")]
        public string SessionName { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
