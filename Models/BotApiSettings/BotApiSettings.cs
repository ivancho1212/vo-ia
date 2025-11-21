using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_api_settings")]
    public class BotApiSettings
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }
        
        public Bot Bot { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("client_secret")]
        public string ClientSecret { get; set; }

        [Column("allowed_origins")]
        public string AllowedOrigins { get; set; }  // "https://client-a.com,https://app.client-a.com"

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
