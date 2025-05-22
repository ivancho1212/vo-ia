using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotDataCaptureFieldCreateDto
    {
        [Required]
        public int BotId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; }

        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; }

        public bool? IsRequired { get; set; } = false;
    }
}
