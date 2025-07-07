namespace Voia.Api.Models.BotConversation
{
    public class FilePayloadWrapper
    {
        public FilePayload File { get; set; }
        public List<FilePayload> MultipleFiles { get; set; }
    }
}
