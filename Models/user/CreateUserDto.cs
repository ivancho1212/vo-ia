using System.ComponentModel.DataAnnotations;

namespace Voia.Api.Models.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico es inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public int DocumentTypeId { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^3\d{9}$", ErrorMessage = "Teléfono inválido. Debe comenzar con 3 y tener 10 dígitos.")]
        public string Phone { get; set; }
        // 🔹 Nuevos campos
        [Required]
        public string Country { get; set; }

        [Required]
        public string City { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^\d{6,12}$", ErrorMessage = "Documento inválido (6 a 12 dígitos).")]
        public string DocumentNumber { get; set; }

        public string DocumentPhotoUrl { get; set; }

        public string AvatarUrl { get; set; }

        public bool IsVerified { get; set; } = false;


        // ✅ Consentimientos
        [Required(ErrorMessage = "Debes aceptar los Términos y Condiciones.")]
        public bool AcceptTerms { get; set; }  // obligatorio

        public bool AllowAiTraining { get; set; } = false; // opcional
    }
}
