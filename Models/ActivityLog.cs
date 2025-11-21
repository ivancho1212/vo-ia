using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("activity_logs")]
    public class ActivityLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("action")]
        [StringLength(100)]
        public string? Action { get; set; }

        [Column("entity_type")]
        [StringLength(100)]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("old_values")]
        public string? OldValues { get; set; }

        [Column("new_values")]
        public string? NewValues { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        [StringLength(500)]
        public string? UserAgent { get; set; }

        [Column("request_id")]
        [StringLength(50)]
        public string? RequestId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
