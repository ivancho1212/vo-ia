using System.ComponentModel.DataAnnotations.Schema;
namespace Voia.Api.Models.DTOs
{
    public class UpdateUserDto
    {
        public string? Status { get; set; } // "active", "blocked", "inactive"
        public int RoleId { get; set; }
        public int? DocumentTypeId { get; set; }
        public int Id { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // 🔹 Nuevos campos
    public string? Country { get; set; }
    public string? City { get; set; }

    public string? Address { get; set; }
        public string DocumentNumber { get; set; }
    public string DocumentPhotoUrl { get; set; }
    public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
    }

}
