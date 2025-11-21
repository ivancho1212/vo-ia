namespace Voia.Api.Models
{
    /// <summary>
    /// Entidad para almacenar refresh tokens
    /// 
    /// Propósito:
    /// - Persistir tokens en BD para validación
    /// - Implementar revocación en logout
    /// - Rastrear sesiones activas por usuario
    /// - Auditoría y seguridad
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Identificador único del registro
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario propietario del token (FK a AspNetUsers)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Token refresh aleatorio (256 bits, base64)
        /// Se envía en httpOnly cookie
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// JWT ID (jti claim) del token
        /// Usado para revocación inmediata
        /// </summary>
        public string TokenJti { get; set; }

        /// <summary>
        /// Fecha de expiración del token
        /// Por defecto: 7 días desde creación
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Indicador de revocación
        /// True si fue revocado en logout o cambio de contraseña
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Timestamp de creación
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Dirección IP de la sesión
        /// Para auditoría de seguridad
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User-Agent del cliente
        /// Para auditoría de seguridad
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Timestamp de último uso del token
        /// Opcional, para tracking de actividad
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }
}
