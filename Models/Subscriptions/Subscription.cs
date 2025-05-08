using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;

namespace Voia.Api.Models.Subscriptions
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("Plan")]
        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; } = DateTime.UtcNow;

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [MaxLength(10)]
        public string Status { get; set; } = "active";

        // Relaciones
        public User? User { get; set; }
        public Plan? Plan { get; set; }
    }
}
