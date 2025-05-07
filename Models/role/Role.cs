using System.Text.Json.Serialization;

namespace Voia.Api.Models
{
    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        
        [JsonIgnore]  // Evitar la serialización de Users
        public ICollection<User> Users { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }

        public string? Description { get; set; }
    }
}
