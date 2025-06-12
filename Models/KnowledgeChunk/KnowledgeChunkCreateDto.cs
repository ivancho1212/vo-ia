namespace Voia.Api.Models.Dtos
{
    public class KnowledgeChunkCreateDto
    {
        public int UploadedDocumentId { get; set; }
        public string Content { get; set; }
        public string Metadata { get; set; }
        public byte[] EmbeddingVector { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
    }
}
