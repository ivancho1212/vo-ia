using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Users;

namespace Voia.Api.Models.Messages
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; } // ✅ Ahora sí acepta null


    [Column("user_id")]
    public int? UserId { get; set; }

        [Required]
        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [Required]
        [Column("sender")]
        public string Sender { get; set; } = "user"; // user | bot | admin

        [Required]
        [Column("message_text")]
        public string MessageText { get; set; }

        [Column("tokens_used")]
        public int? TokensUsed { get; set; } = 0;

        [Column("source")]
        public string Source { get; set; } = "widget"; // widget | admin-panel

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("read")]
        public bool Read { get; set; } = false;


        // ✅ Nuevo campo para soportar respuestas
        [Column("reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }

        [ForeignKey("ReplyToMessageId")]
        public virtual Message? ReplyToMessage { get; set; }

        // Relaciones existentes
        public virtual Bot Bot { get; set; }
        public virtual User User { get; set; }
        public virtual Conversation Conversation { get; set; }
        [Column("public_user_id")]
        public int? PublicUserId { get; set; }

        [ForeignKey("PublicUserId")]
        public virtual PublicUser? PublicUser { get; set; }

    }
}
