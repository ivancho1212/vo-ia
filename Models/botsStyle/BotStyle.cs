using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class BotStyle
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; } // ← Nuevo campo agregado
        public string? Name { get; set; } // ← Nuevo campo agregado

        public int? StyleTemplateId { get; set; }

        public string Theme { get; set; } = "light";

        [MaxLength(20)]
        public string PrimaryColor { get; set; } = "#000000";

        [MaxLength(20)]
        public string SecondaryColor { get; set; } = "#ffffff";

        [MaxLength(100)]
        public string FontFamily { get; set; } = "Arial";

        public string AvatarUrl { get; set; }

        public string Position { get; set; } = "bottom-right";

        public string CustomCss { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }

        [Column("header_background_color")]
        public string? HeaderBackgroundColor { get; set; }
        [Column("allow_image_upload")]
        public bool AllowImageUpload { get; set; } = true;

        [Column("allow_file_upload")]
        public bool AllowFileUpload { get; set; } = true;
        public string? Title { get; set; }


    }

}
