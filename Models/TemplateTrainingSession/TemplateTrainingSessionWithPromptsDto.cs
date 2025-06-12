using System.Collections.Generic;
using Voia.Api.Models;

namespace Voia.Api.Models.Dtos
{
    public class TemplateTrainingSessionWithPromptsDto
    {
        public int BotTemplateId { get; set; }
        public string SessionName { get; set; }
        public string Description { get; set; }
        public List<BotCustomPromptDto> Prompts { get; set; }

        // Nuevos campos
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public string? SourceLink { get; set; }
    }
}
