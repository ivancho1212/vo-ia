using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_template_prompts")]
    public class BotTemplatePrompt
    {
        public int Id { get; set; }

        [ForeignKey("BotTemplate")]
        [Column("bot_template_id")]
        public int BotTemplateId { get; set; }
        public BotTemplate BotTemplate { get; set; }

        [Required]
        [EnumDataType(typeof(PromptRole))]
        public PromptRole Role { get; set; }

        [Required]
        public string Content { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PromptRole
    {
        system,
        user,
        assistant
    }
}
