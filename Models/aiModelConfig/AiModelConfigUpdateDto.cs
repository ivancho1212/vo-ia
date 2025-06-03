using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.AiModelConfigs
{
    public class AiModelConfigUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; }

        public decimal? Temperature { get; set; } = 0.70m;
        public decimal? FrequencyPenalty { get; set; } = 0.00m;
        public decimal? PresencePenalty { get; set; } = 0.00m;
    }
}
