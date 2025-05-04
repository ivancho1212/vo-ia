using System;
using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.SupportTicket
{
    public class UpdateSupportTicketDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [RegularExpression("open|in_progress|closed", ErrorMessage = "Status must be 'open', 'in_progress' or 'closed'.")]
        public string Status { get; set; }
    }
}
