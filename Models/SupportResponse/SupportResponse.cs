using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.SupportTicket
{
    [Table("support_responses")]
    public class SupportResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("SupportTicket")]
        [Column("ticket_id")]

        public int TicketId { get; set; }

        [ForeignKey("User")]
        [Column("responder_id")]

        public int? ResponderId { get; set; }

        [Required]
        public string Message { get; set; }

        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
