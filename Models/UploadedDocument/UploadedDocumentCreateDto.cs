namespace Voia.Api.Models.Dtos
{
    public class UploadedDocumentCreateDto
    {
        public int BotTemplateId { get; set; }
        public int? TemplateTrainingSessionId { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; }
        public string? FileType { get; set; }
        public string FilePath { get; set; }
    }
}
