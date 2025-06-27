using System;

namespace Voia.Api.Models.Dtos
{
    public class UploadedDocumentResponseDto
    {
        public int Id { get; set; }
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; }
        public string? FileType { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool? Indexed { get; set; }
        public string? ContentHash { get; set; }
        public string? QdrantId { get; set; }
        public string? ExtractedText { get; set; }

    }
}
