using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class TemplateTrainingSession
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }

        [Required]
        [Column("session_name")]
        public string SessionName { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public BotTemplate BotTemplate { get; set; }

        // ✅ Esta propiedad es esencial para la relación inversa
        public ICollection<BotCustomPrompt> BotCustomPrompts { get; set; }
    }
}
