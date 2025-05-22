// Models/BotCustomPrompt/BotCustomPromptUpdateDto.cs
using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class BotCustomPromptUpdateDto
    {
        [Required]
        [RegularExpression("system|user|assistant")]
        public string Role { get; set; }

        [Required]
        public string Content { get; set; }

        public int? TrainingSessionId { get; set; }
    }
}
