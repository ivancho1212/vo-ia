using System;

namespace Voia.Api.Models.DTOs
{
    public class BotTemplatePromptResponseDto
    {
        public int Id { get; set; }
        public int BotTemplateId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
