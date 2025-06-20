using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Dtos
{
    public class UploadedDocumentCreateDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public int BotTemplateId { get; set; }

        public int? TemplateTrainingSessionId { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
