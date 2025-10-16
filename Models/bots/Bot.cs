using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models;
using Voia.Api.Models.AiModelConfigs;
using Voia.Api.Models.BotTrainingSession;

namespace Voia.Api.Models
{
    [Table("Bots")] // <- Asegúrate que el nombre de la tabla esté correcto
    public class Bot
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("api_key")]
        public string ApiKey { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("is_ready")]
        public bool IsReady { get; set; } = false;

        // Usuario (requerido)
        [Column("user_id")]
        public int UserId { get; set; }
        public User User { get; set; }

        // Estilo (opcional)
        [Column("style_id")]
        public int? StyleId { get; set; }
        public BotStyle Style { get; set; }

        // Proveedor IA (requerido)
        [Column("ia_provider_id")]
        public int IaProviderId { get; set; }
        public BotIaProvider IaProvider { get; set; }

        // Config modelo IA (opcional)
        [Column("ai_model_config_id")]
        public int? AiModelConfigId { get; set; }
        public AiModelConfig AiModelConfig { get; set; }

        // Plantilla (opcional)
        [Column("bot_template_id")]
        public int? BotTemplateId { get; set; }
        public BotTemplate BotTemplate { get; set; }

        // RELACIÓN UNO-A-MUCHOS: un Bot tiene varias BotTrainingSession
        public List<BotTrainingSession.BotTrainingSession> TrainingSessions { get; set; } = new List<BotTrainingSession.BotTrainingSession>();
    }

}
