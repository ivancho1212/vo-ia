using System;
using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.SupportTicket
{
    public class CreateSupportTicketDto
    {
        [Required]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } = "open"; // Valor por defecto

        public DateTime? CreatedAt { get; set; }  // La base de datos maneja esto automáticamente
        public DateTime? UpdatedAt { get; set; }  // La base de datos maneja esto automáticamente
    }
}
