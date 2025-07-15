using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations; // ✅ Asegúrate de importar el namespace correcto

namespace Voia.Api.Models.Chat
{
    [Table("chat_uploaded_files")]
    public class ChatUploadedFile
    {
        [Key]
        public int Id { get; set; }

        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("file_name")]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Column("file_type")]
        [MaxLength(50)]
        public string FileType { get; set; }

        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; } = DateTime.UtcNow;

        // ✅ Relación con conversación
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }
        
        [ForeignKey("UserId")] // ✅ Esto faltaba
        public User? User { get; set; }
    }
}
