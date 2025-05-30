using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class Bot
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string ApiKey { get; set; }
        public string ModelUsed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } // Navegación
    }


}