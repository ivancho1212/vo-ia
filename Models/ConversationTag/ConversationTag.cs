using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Messages;

namespace Voia.Api.Models.ConversationTag
{
    [Table("conversation_tags")]
    public class ConversationTag
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [ForeignKey("ConversationId")]
        public virtual Conversation? Conversation { get; set; }

        [Column("label")]
        public string Label { get; set; } = string.Empty;

        [Column("highlighted_message_id")]
        public int? HighlightedMessageId { get; set; }

        [ForeignKey("HighlightedMessageId")]
        public virtual Message? HighlightedMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
