using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("token_usage_logs")]
    public class TokenUsageLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }

        [Required]
        [Column("tokens_used")]
        public int TokensUsed { get; set; }

        [Column("usage_date")]
        public DateTime? UsageDate { get; set; }
    }
}
