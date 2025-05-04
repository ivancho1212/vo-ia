using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.TrainingDataSessions
{
    [Table("training_data_sessions")]
    public class TrainingDataSession
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; }

        [Column("data_summary")]
        public string? DataSummary { get; set; }

        [Column("data_type")]
        public string? DataType { get; set; } = "text";

        [Column("status")]
        public string? Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
