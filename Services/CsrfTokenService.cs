using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Voia.Api.Services
{
    /// <summary>
    /// Servicio para generar y validar CSRF tokens
    /// </summary>
    public interface ICsrfTokenService
    {
        /// <summary>
        /// Genera un nuevo CSRF token único
        /// </summary>
        string GenerateToken();

        /// <summary>
        /// Valida que un token CSRF sea válido
        /// </summary>
        bool ValidateToken(string token);

        /// <summary>
        /// Obtiene el nombre de la cookie donde se almacena el token
        /// </summary>
        string GetCookieName();

        /// <summary>
        /// Obtiene el nombre del header donde se envía el token
        /// </summary>
        string GetHeaderName();
    }

    public class CsrfTokenService : ICsrfTokenService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CsrfTokenService> _logger;
        private readonly int _tokenExpirationMinutes = 60;
        private const string CookieName = "X-CSRF-Token";
        private const string HeaderName = "X-CSRF-Token";
        private const string CacheKeyPrefix = "csrf_token_";

        public CsrfTokenService(IMemoryCache cache, ILogger<CsrfTokenService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public string GenerateToken()
        {
            // Generar token aleatorio de 32 bytes (256 bits)
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // Convertir a base64 para almacenamiento
            string token = Convert.ToBase64String(tokenBytes);

            // Almacenar en caché con expiración
            _cache.Set(
                CacheKeyPrefix + token,
                true,
                TimeSpan.FromMinutes(_tokenExpirationMinutes)
            );

            _logger.LogInformation($"CSRF token generado: {token.Substring(0, 10)}...");
            return token;
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("CSRF token vacío");
                return false;
            }

            // Buscar en caché
            string cacheKey = CacheKeyPrefix + token;
            bool isValid = _cache.TryGetValue(cacheKey, out _);

            if (!isValid)
            {
                _logger.LogWarning($"CSRF token inválido o expirado: {token.Substring(0, 10)}...");
                return false;
            }

            // Token validado, eliminarlo del caché (one-time use)
            _cache.Remove(cacheKey);
            _logger.LogInformation($"CSRF token validado y consumido: {token.Substring(0, 10)}...");
            return true;
        }

        public string GetCookieName() => CookieName;
        public string GetHeaderName() => HeaderName;
    }
}
