using System;

namespace Voia.Api.Models.Messages
{
    public class ConversationItemDto
    {
        // ✅ PROPIEDADES QUE FALTABAN:
        public int Id { get; set; }
        public int? ReplyToMessageId { get; set; }
        
        // --- Propiedades existentes ---
        public string Type { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileUrl { get; set; }
        public DateTime Timestamp { get; set; }
        public int? FromId { get; set; }
        public string FromName { get; set; }
        public string FromRole { get; set; }
        public string FromAvatarUrl { get; set; }
    }
}