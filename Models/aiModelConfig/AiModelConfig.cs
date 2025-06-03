using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.AiModelConfigs
{
    [Table("ai_model_configs")]
    public class AiModelConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("model_name")]
        [MaxLength(100)]
        public string ModelName { get; set; }

        [Column("temperature", TypeName = "decimal(3,2)")]
        public decimal? Temperature { get; set; } = 0.70m;

        [Column("frequency_penalty", TypeName = "decimal(3,2)")]
        public decimal? FrequencyPenalty { get; set; } = 0.00m;

        [Column("presence_penalty", TypeName = "decimal(3,2)")]
        public decimal? PresencePenalty { get; set; } = 0.00m;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("ia_provider_id")]
        public int IaProviderId { get; set; } // <-- nuevo campo obligatorio
    }
}
