namespace Voia.Api.Models.DTOs
{
    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } // Agregar esta propiedad

    }
}
