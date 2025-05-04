using System.ComponentModel.DataAnnotations.Schema;
namespace Voia.Api.Models.DTOs
{
    public class UpdateUserDto
    {
        public int Id { get; set; }  // Aquí agregamos el campo Id

        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentPhotoUrl { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
    }
}
