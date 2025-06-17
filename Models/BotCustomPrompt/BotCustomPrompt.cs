using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class BotCustomPrompt
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("bot_template_id")]
    public int BotTemplateId { get; set; }

    [ForeignKey("BotTemplateId")]
    public BotTemplate BotTemplate { get; set; }

    [Required]
    [Column("role")]
    public string Role { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; }

    [Column("template_training_session_id")]
    public int? TemplateTrainingSessionId { get; set; }

    [ForeignKey("TemplateTrainingSessionId")]
    public TemplateTrainingSession? TemplateTrainingSession { get; set; }

    // ✅ Nuevo: Asociación con el bot
    [Column("bot_id")]
    public int? BotId { get; set; }

    [ForeignKey("BotId")]
    public Bot? Bot { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

}
