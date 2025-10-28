namespace Voia.Api.Models.Upload
{
    public class PresignedRequestDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int ConversationId { get; set; }
        public int? UserId { get; set; }
    }
}
