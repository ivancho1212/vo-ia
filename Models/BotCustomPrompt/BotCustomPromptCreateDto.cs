using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models
{
    public class BotCustomPromptCreateDto
    {
        [Required]
        public int BotId { get; set; }
        
        public int? BotTemplateId { get; set; }
        
        [Required]
        public string Role { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public int? TemplateTrainingSessionId { get; set; }
    }
}
