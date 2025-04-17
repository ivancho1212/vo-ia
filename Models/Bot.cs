using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class Bot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BotName { get; set; }

        public string Description { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public User User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
