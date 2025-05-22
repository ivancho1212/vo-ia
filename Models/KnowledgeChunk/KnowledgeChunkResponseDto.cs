namespace Voia.Api.Models.Dtos
{
    public class KnowledgeChunkResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public string Content { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
