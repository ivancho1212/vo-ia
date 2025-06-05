using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column("default_style_id")]
        public int? DefaultStyleId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // 👇 Agrega esta línea
        public ICollection<BotTemplatePrompt> Prompts { get; set; } = new List<BotTemplatePrompt>();
    }
}
