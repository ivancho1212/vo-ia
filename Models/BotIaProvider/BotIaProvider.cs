using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_ia_providers")]
    public class BotIaProvider
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("api_endpoint")]
        public string ApiEndpoint { get; set; }

        [MaxLength(255)]
        [Column("api_key")]
        public string ApiKey { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
