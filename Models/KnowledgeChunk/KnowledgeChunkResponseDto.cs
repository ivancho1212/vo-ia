using System;

namespace Voia.Api.Models.Dtos
{
    public class KnowledgeChunkResponseDto
    {
        public int Id { get; set; }
        public int UploadedDocumentId { get; set; }
        public string Content { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
    }
}
