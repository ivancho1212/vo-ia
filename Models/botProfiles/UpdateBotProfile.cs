using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.BotProfiles
{
    public class UpdateBotProfile
    {
        [Required]
        public int BotId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Url]
        public string AvatarUrl { get; set; }

        [MaxLength(500)]
        public string Bio { get; set; }

        [MaxLength(200)]
        public string PersonalityTraits { get; set; }

        [MaxLength(10)]
        public string Language { get; set; }

        [MaxLength(50)]
        public string Tone { get; set; }

        [MaxLength(300)]
        public string Restrictions { get; set; }
    }
}
