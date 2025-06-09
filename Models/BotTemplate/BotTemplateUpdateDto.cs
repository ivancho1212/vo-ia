using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Voia.Api.Models.DTOs; // Add the namespace containing BotTemplatePromptUpdateDto

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

        public List<BotTemplatePromptUpdateDto> Prompts { get; set; } = new();
    }
}
