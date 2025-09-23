using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.Bots; // 👈 Asegúrate de tener el namespace correcto

namespace Voia.Api.Models
{
    [Table("users")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        public int? DocumentTypeId { get; set; }
        public string Phone { get; set; }

        // 🔹 Nuevos campos
        public string? Country { get; set; }
        public string? City { get; set; }

        public string? Address { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentPhotoUrl { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 🔹 Relaciones
        public Role Role { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<UserConsent> Consents { get; set; }

        // 🚀 Nueva relación con Bots
        public ICollection<Bot> Bots { get; set; } = new List<Bot>();
    }
}
