using System;

namespace Voia.Api.Models.DTOs
{
    public class BotDataCaptureFieldResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public bool? IsRequired { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
