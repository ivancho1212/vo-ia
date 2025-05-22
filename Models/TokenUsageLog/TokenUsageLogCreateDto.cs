using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class TokenUsageLogCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int BotId { get; set; }

        [Required]
        public int TokensUsed { get; set; }
    }
}
