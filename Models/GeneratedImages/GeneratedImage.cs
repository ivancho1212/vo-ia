using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.GeneratedImages
{
    [Table("generated_images")]
    public class GeneratedImage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; }

        [Required]
        [Column("prompt")]
        public string Prompt { get; set; }

        [Required]
        [Column("image_url")]
        public string ImageUrl { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
