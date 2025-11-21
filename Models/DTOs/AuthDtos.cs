namespace Voia.Api.Models.DTOs
{
    /// <summary>
    /// Request para login con email y contraseña
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Response exitoso de login
    /// Contiene access token (refresh token está en httpOnly cookie)
    /// </summary>
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // En segundos
        public UserInfoDto User { get; set; }
    }

    /// <summary>
    /// Response exitoso de refresh
    /// Contiene nuevo access token y refresh token actualizado (en cookie)
    /// </summary>
    public class RefreshResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // En segundos
    }

    /// <summary>
    /// Información del usuario logueado
    /// </summary>
    public class UserInfoDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public string? UserName { get; set; }
    }

    /// <summary>
    /// Response de logout
    /// </summary>
    public class LogoutResponse
    {
        public string Message { get; set; } = "Logged out successfully";
    }
}
