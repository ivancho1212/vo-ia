namespace Voia.Api.Models.DTOs
{
    public class UpdateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Permissions { get; set; } // Agregar esta propiedad
    }
}
