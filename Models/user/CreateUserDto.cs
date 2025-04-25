using System.ComponentModel.DataAnnotations.Schema;

// Ubicación: Models/User.cs (fuera de la carpeta DTOs)
namespace Voia.Api.Models.DTOs
{
    public class CreateUserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }
        public int? DocumentTypeId { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentPhotoUrl { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
    }
}
