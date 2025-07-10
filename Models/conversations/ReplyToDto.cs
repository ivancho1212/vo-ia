namespace Voia.Api.Models.Conversations
{
    public class ReplyToDto
    {
        public string? Id { get; set; }            // ID del mensaje original
        public string? From { get; set; }          // "user", "admin", etc.
        public string? Text { get; set; }          // Texto del mensaje original
        public string? Timestamp { get; set; }     // Timestamp como string ISO
    }
}
