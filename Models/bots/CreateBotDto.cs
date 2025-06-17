using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.Bots
{
    public class CreateBotDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string ApiKey { get; set; }

        public string ModelUsed { get; set; } = "gpt-4";

        public bool IsActive { get; set; } = true;

        [Required]
        public int BotTemplateId { get; set; }

        public string? CustomText { get; set; }
    }
}
