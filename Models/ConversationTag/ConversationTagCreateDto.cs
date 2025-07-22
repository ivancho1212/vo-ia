namespace Voia.Api.Models.ConversationTag
{
    public class ConversationTagCreateDto
    {
        public int ConversationId { get; set; }
        public string Label { get; set; }
        public int? HighlightedMessageId { get; set; }
    }
}
