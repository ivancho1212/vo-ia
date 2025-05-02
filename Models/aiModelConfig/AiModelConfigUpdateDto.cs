using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.AiModelConfigs
{
    public class AiModelConfigUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int BotId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; }

        public decimal? Temperature { get; set; } = 0.70m;
        public int? MaxTokens { get; set; } = 512;
        public decimal? FrequencyPenalty { get; set; } = 0.00m;
        public decimal? PresencePenalty { get; set; } = 0.00m;
    }
}
