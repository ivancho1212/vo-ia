using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models; // Para BotIaProvider y BotStyle
using Voia.Api.Models.AiModelConfigs; // Para AiModelConfig

namespace Voia.Api.Models
{
    [Table("bot_templates")]
    public class BotTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Column("ia_provider_id")]
        [Required]
        public int IaProviderId { get; set; }

        [ForeignKey("IaProviderId")]
        public BotIaProvider IaProvider { get; set; }  // ✅ Navegación

        [Column("ai_model_config_id")]
        [Required]
        public int AiModelConfigId { get; set; }

        [ForeignKey("AiModelConfigId")]
        public AiModelConfig AiModelConfig { get; set; }  // ✅ Navegación

        [Column("default_style_id")]
        public int? DefaultStyleId { get; set; }

        [ForeignKey("DefaultStyleId")]
        public BotStyle? DefaultStyle { get; set; }  // ✅ Navegación

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<BotTemplatePrompt> Prompts { get; set; } = new List<BotTemplatePrompt>();
    }
}
