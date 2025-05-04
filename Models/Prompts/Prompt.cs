using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;

namespace Voia.Api.Models.Prompts
{
    public class Prompt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("conversation_id")]
        public int? ConversationId { get; set; }

        [Required]
        [Column("prompt_text")]
        public string PromptText { get; set; }

        [Column("response_text")]
        public string ResponseText { get; set; }

        [Column("tokens_used")]
        public int? TokensUsed { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("source")]
        public string Source { get; set; } = "widget";

        // Relaciones
        public virtual Bot Bot { get; set; }
        public virtual User User { get; set; }
        public virtual Conversation Conversation { get; set; }
    }
}
