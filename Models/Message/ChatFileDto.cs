namespace Voia.Api.Models.Messages
{
    public class ChatFileDto
    {
        public int UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileContent { get; set; } = string.Empty;
    }
}
