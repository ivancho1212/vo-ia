using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("rolepermissions")]
    public class RolePermission
    {
        [Column("role_id")]
        public int RoleId { get; set; }
        [Column("permission_id")]
        public Role Role { get; set; }

        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}
