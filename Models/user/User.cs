using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.Bots; // 👈 Asegúrate de tener el namespace correcto

namespace Voia.Api.Models
{
    // Usar tabla estándar de Identity
    public class User : Microsoft.AspNetCore.Identity.IdentityUser<int>
    {
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentPhotoUrl { get; set; }
        public string? Phone { get; set; }
        public int? DocumentTypeId { get; set; }
        public DocumentType? DocumentType { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PublicDataToken { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Relaciones y claves foráneas
        public int? RoleId { get; set; }
        public Role? Role { get; set; }
        public ICollection<Subscription>? Subscriptions { get; set; }
        public ICollection<Bot>? Bots { get; set; }
    }
}
