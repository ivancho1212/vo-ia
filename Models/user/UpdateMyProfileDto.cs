// Models/DTOs/UpdateMyProfileDto.cs
namespace Voia.Api.Models.DTOs
{
    public class UpdateMyProfileDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string DocumentNumber { get; set; } = null!;
    }
}
