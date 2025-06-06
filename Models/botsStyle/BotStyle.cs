using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class BotStyle
    {
        [Key]
        public int Id { get; set; }

        // Nuevo campo
        public int? StyleTemplateId { get; set; }

        // Para theme y position, se puede usar enum o string. Aquí uso string para simplicidad.
        public string Theme { get; set; } = "light"; // default

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
    }
}
