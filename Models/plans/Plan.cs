using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.Plans
{
    public class Plan
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0, 9999999999.99)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("max_tokens")]
        public int MaxTokens { get; set; }

        [Column("bots_limit")]
        public int? BotsLimit { get; set; }

        [Column("file_upload_limit")]
        public int? FileUploadLimit { get; set; }

        [Column("ai_providers", TypeName = "json")]
        public string? AiProviders { get; set; } // Serializado como JSON

        [Column("custom_styles")]
        public bool CustomStyles { get; set; }

        [Column("data_capture_limit")]
        public int? DataCaptureLimit { get; set; }

        [Column("analytics_dashboard")]
        public bool AnalyticsDashboard { get; set; }

        [Column("priority_support")]
        public bool PrioritySupport { get; set; }

        [Column("integration_api")]
        public bool IntegrationApi { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}