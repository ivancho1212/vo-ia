using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotTemplateUpdateDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int? IaProviderId { get; set; }

        public int? AiModelConfigId { get; set; }

        public int? DefaultStyleId { get; set; }
    }
}
