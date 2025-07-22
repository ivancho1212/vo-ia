using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Messages;

namespace Voia.Api.Models.ConversationTag
{
    public class ConversationTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public string Label { get; set; } = string.Empty;

        public string? Color { get; set; }

        public int? HighlightedMessageId { get; set; }

        [ForeignKey("ConversationId")]
        public Conversation? Conversation { get; set; }

        [ForeignKey("HighlightedMessageId")]
        public Message? HighlightedMessage { get; set; }

        // 🆕 Propiedades de auditoría
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
