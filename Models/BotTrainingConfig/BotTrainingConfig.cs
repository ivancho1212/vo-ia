using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models;

[Table("bot_training_configs")]
public class BotTrainingConfig
{
    public int Id { get; set; }
    [Column("bot_id")]
    public int BotId { get; set; }
    [Column("training_type")]
    public string TrainingType { get; set; } // Enum: "url", "form_data", "manual_prompt", "document"

    public string Data { get; set; } // JSON o texto plano
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Bot Bot { get; set; } // Navigation property (opcional)
}
