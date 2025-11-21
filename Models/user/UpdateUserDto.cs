using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.DTOs
{
    public class UpdateUserDto
    {
        [StringLength(20, MinimumLength = 0)]
        public string? Status { get; set; } // "active", "blocked", "inactive"
        
        public string? RoleName { get; set; }
        
        public int? DocumentTypeId { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Id debe ser válido")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z\s\-áéíóúñÁÉÍÓÚÑ']+$", ErrorMessage = "El nombre solo puede contener letras, espacios, guiones y apóstrofes")]
        public string Name { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "El email debe tener entre 5 y 150 caracteres")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "El teléfono contiene caracteres inválidos")]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 20 caracteres")]
        public string Phone { get; set; }

        // 🔹 Campos adicionales
        [StringLength(100, ErrorMessage = "El país no debe exceder 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z\s\-áéíóúñÁÉÍÓÚÑ']*$", ErrorMessage = "El país solo puede contener letras")]
        public string? Country { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no debe exceder 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z\s\-áéíóúñÁÉÍÓÚÑ']*$", ErrorMessage = "La ciudad solo puede contener letras")]
        public string? City { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no debe exceder 255 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.,#áéíóúñÁÉÍÓÚÑ']*$", ErrorMessage = "La dirección contiene caracteres inválidos")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "El documento es requerido")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El documento debe tener entre 3 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9\-\/]*$", ErrorMessage = "El documento solo puede contener letras, números, guiones y barras")]
        public string DocumentNumber { get; set; }

        [StringLength(500, ErrorMessage = "La URL del documento no debe exceder 500 caracteres")]
        [Url(ErrorMessage = "La URL del documento no tiene un formato válido")]
        public string DocumentPhotoUrl { get; set; }

        [StringLength(500, ErrorMessage = "La URL del avatar no debe exceder 500 caracteres")]
        [Url(ErrorMessage = "La URL del avatar no tiene un formato válido")]
        public string? AvatarUrl { get; set; }

        public bool IsVerified { get; set; }
    }

}
