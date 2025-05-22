using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotDataSubmissionUpdateDto
    {
        [Required]
        public int BotId { get; set; }

        [Required]
        public int CaptureFieldId { get; set; }

        public string SubmissionValue { get; set; }
    }
}
