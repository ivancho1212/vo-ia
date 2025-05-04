using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.BotProfiles
{
    [Table("bot_profiles")]
    public class BotProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("avatar_url")]
        public string AvatarUrl { get; set; }

        [Column("bio")]
        public string Bio { get; set; }

        [Column("personality_traits")]
        public string PersonalityTraits { get; set; }

        [Column("language")]
        public string Language { get; set; } = "es";  // Valor predeterminado en la base de datos

        [Column("tone")]
        public string Tone { get; set; }

        [Column("restrictions")]
        public string Restrictions { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Se establece solo al crear

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;  // Se actualiza al modificar

        // Relación con Bot
        [ForeignKey("BotId")]
        public virtual Bot Bot { get; set; }
    }
}
