using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
public class User
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    [Column("role_id")] // Esta línea especifica que el nombre de la columna en la base de datos es 'role_id'
    public int RoleId { get; set; }

    public int? DocumentTypeId { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string DocumentNumber { get; set; }
    public string DocumentPhotoUrl { get; set; }
    public string AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; }  // Asegúrate de que la relación con Role esté definida correctamente
}

}
