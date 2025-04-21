using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // Usamos el atributo Column para mapear la columna 'role_id' de la base de datos
        [Column("role_id")]
        public int RoleId { get; set; }     
        public int? DocumentTypeId { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentPhotoUrl { get; set; } // <-- Faltaba
        public string AvatarUrl { get; set; }        // <-- Faltaba
        public bool IsVerified { get; set; }

        public DateTime CreatedAt { get; set; }      // <-- Faltaba
        public DateTime UpdatedAt { get; set; }      // <-- Faltaba

        public virtual Role Role { get; set; }
    }

}
