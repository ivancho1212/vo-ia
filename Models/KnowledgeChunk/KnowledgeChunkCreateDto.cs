using System.ComponentModel.DataAnnotations.Schema;
namespace Voia.Api.Models.Dtos
{
    public class KnowledgeChunkCreateDto
    {
        public int BotId { get; set; }
        public string Content { get; set; }
        public string Metadata { get; set; }
        public byte[] EmbeddingVector { get; set; }
    }
}
