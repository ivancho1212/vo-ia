namespace Voia.Api.Models.Messages
{
    public class ChatFileDto
    {
    public string? FileName { get; set; }
    public string? FileType { get; set; }
        public string? FileContent { get; set; }
        public string? FileUrl { get; set; }
        // Nullable because clients may omit userId (anonymous widget) or send null
        public int? UserId { get; set; }
    }
}
