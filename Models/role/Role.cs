using System.Text.Json.Serialization;

namespace Voia.Api.Models
{
    public class Role : Microsoft.AspNetCore.Identity.IdentityRole<int>
    {
        public string? Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
        [JsonIgnore]  // Evitar la serialización de Users
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
