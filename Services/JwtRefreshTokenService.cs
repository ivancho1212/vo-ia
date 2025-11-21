using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Voia.Api.Services
{
    /// <summary>
    /// Interface para la gestión de JWT tokens y refresh tokens
    /// </summary>
    public interface IJwtRefreshTokenService
    {
        /// <summary>
        /// Genera un refresh token criptográficamente seguro (256 bits)
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Genera un JWT access token firmado con expiration de 15 minutos
        /// </summary>
        string GenerateAccessToken(string userId, string email, string name);

        /// <summary>
        /// Valida y lee un JWT token
        /// </summary>
        JwtSecurityToken ValidateAndReadToken(string token);

        /// <summary>
        /// Revoca un token agregándolo a la blacklist
        /// </summary>
        void RevokeToken(string tokenId);

        /// <summary>
        /// Verifica si un token está en la blacklist
        /// </summary>
        bool IsTokenRevoked(string tokenId);

        /// <summary>
        /// Obtiene el JWT ID (jti) claim de un token
        /// </summary>
        string GetTokenJti(string token);
    }

    /// <summary>
    /// Servicio para generar, validar y revocar JWT tokens
    /// 
    /// Tokens generados:
    /// - Access Token: 15 minutos de vida, se envía en Authorization header
    /// - Refresh Token: 7 días de vida, se guarda en httpOnly cookie
    /// 
    /// Seguridad:
    /// - Access tokens cortos previenen daño en caso de robo
    /// - Refresh tokens en httpOnly previene robo por XSS
    /// - Revocación inmediata en logout
    /// </summary>
    public class JwtRefreshTokenService : IJwtRefreshTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtRefreshTokenService> _logger;
        
        // En producción: usar Redis en lugar de HashSet en memoria
        private readonly HashSet<string> _revokedTokens;

        public JwtRefreshTokenService(
            IConfiguration configuration,
            ILogger<JwtRefreshTokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _revokedTokens = new HashSet<string>();
        }

        /// <summary>
        /// Genera un refresh token aleatorio de 256 bits (32 bytes)
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var token = Convert.ToBase64String(randomNumber);
                _logger.LogDebug("Refresh token generado exitosamente");
                return token;
            }
        }

        /// <summary>
        /// Genera un JWT access token con expiration de 15 minutos
        /// </summary>
        public string GenerateAccessToken(string userId, string email, string name)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            try
            {
                var secretKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
                var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim("sub", userId),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, name),
                    new Claim("jti", Guid.NewGuid().ToString()) // JWT ID para revocación
                };

                var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes),
                    signingCredentials: signingCredentials);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogDebug($"Access token generado para usuario: {userId}");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando access token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Valida un JWT token contra la clave secreta y claims esperados
        /// </summary>
        public JwtSecurityToken ValidateAndReadToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
                }, out SecurityToken validatedToken);

                _logger.LogDebug("Token validado exitosamente");
                return (JwtSecurityToken)validatedToken;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning($"Token expirado: {ex.Message}");
                throw;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning($"Token con firma inválida: {ex.Message}");
                throw;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning($"Error validando token: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado validando token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Revoca un token agregando su JWT ID a la blacklist
        /// En producción: persistir en Redis con expiración
        /// </summary>
        public void RevokeToken(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                throw new ArgumentNullException(nameof(tokenId));

            _revokedTokens.Add(tokenId);
            _logger.LogInformation($"Token revocado: {tokenId}");

            // TODO: En producción, guardar en Redis:
            // await _redis.GetDatabase().StringSetAsync(
            //     $"revoked_token:{tokenId}",
            //     "1",
            //     expiry: TimeSpan.FromDays(7));
        }

        /// <summary>
        /// Verifica si un token está en la blacklist
        /// </summary>
        public bool IsTokenRevoked(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return false;

            var isRevoked = _revokedTokens.Contains(tokenId);
            if (isRevoked)
                _logger.LogWarning($"Intento de usar token revocado: {tokenId}");

            return isRevoked;
        }

        /// <summary>
        /// Extrae el JWT ID (jti) claim de un token sin validación completa
        /// Útil para revocar tokens sin necesidad de validar completamente
        /// </summary>
        public string GetTokenJti(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
                
                if (string.IsNullOrEmpty(jti))
                    _logger.LogWarning("Token no contiene claim 'jti'");

                return jti ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leyendo jti del token: {ex.Message}");
                throw;
            }
        }
    }
}
