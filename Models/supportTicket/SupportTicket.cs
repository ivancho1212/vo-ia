using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.SupportTicket
{
    [Table("support_tickets")]
    public class SupportTicket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("subject")]
        public string Subject { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; }

        [Required]
        [Column("status", TypeName = "enum('open','in_progress','closed')")]
        public string Status { get; set; } = "open";

        [Required]
        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }
    }
}
