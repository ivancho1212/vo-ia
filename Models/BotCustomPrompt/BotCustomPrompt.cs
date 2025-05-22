// Models/BotCustomPrompt/BotCustomPrompt.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_custom_prompts")]
    public class BotCustomPrompt
    {
        public int Id { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }

        [Required]
        public string Role { get; set; }  // <-- Usa string

        [Required]
        public string Content { get; set; }
        [Column("training_session_id")]
        public int? TrainingSessionId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
