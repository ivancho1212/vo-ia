using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Users;
using Voia.Api.Models.Chat;

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

        [Column("message_text")]
        public string? MessageText { get; set; }

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

        // ✅ NUEVO: ID temporal del cliente (UUID) para vincular upload con mensaje
        [Column("temp_id")]
        [MaxLength(50)]
        public string? TempId { get; set; }

        // ✅ NUEVO: ID del archivo vinculado a este mensaje
        [Column("file_id")]
        public int? FileId { get; set; }

        [ForeignKey("FileId")]
        public virtual ChatUploadedFile? ChatUploadedFile { get; set; }

        // ✅ NUEVO: Estado del mensaje para arquitectura profesional
        [Column("status")]
        [MaxLength(20)]
        public string? Status { get; set; } = "sent"; // pending, uploading, sent, failed

        // Relaciones existentes
        public virtual Bot? Bot { get; set; }
        public virtual User? User { get; set; }
        public virtual Conversation? Conversation { get; set; }
        [Column("public_user_id")]
        public int? PublicUserId { get; set; }

        [ForeignKey("PublicUserId")]
        public virtual PublicUser? PublicUser { get; set; }
    }
}
