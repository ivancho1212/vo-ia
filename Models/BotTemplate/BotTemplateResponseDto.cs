using System;
using System.Collections.Generic;

namespace Voia.Api.Models.DTOs
{
    public class BotTemplateResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int IaProviderId { get; set; }
        public int? DefaultStyleId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 👇 Agrega esta propiedad para que funcione el mapeo en el controlador
        public List<BotTemplatePromptResponseDto> Prompts { get; set; } = new();
    }
}
