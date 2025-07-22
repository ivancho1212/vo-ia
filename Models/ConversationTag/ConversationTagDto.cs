using System;

namespace Voia.Api.Models.ConversationTag
{
    public class ConversationTagDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Label { get; set; } = string.Empty;
        public int? HighlightedMessageId { get; set; }
        public string? HighlightedMessageText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
