using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.StyleTemplate
{
    [Table("style_templates")]
    public class StyleTemplate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        public string Name { get; set; }

        [Column("theme")]
        public string Theme { get; set; } = "light";

        [Column("primary_color")]
        public string PrimaryColor { get; set; } = "#000000";

        [Column("secondary_color")]
        public string SecondaryColor { get; set; } = "#ffffff";

        [Column("font_family")]
        public string FontFamily { get; set; } = "Arial";

        [Column("avatar_url")]
        public string AvatarUrl { get; set; }

        [Column("position")]
        public string Position { get; set; } = "bottom-right";

        [Column("custom_css")]
        public string CustomCss { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
