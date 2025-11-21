using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Voia.Api.Services.Caching
{
    /// <summary>
    /// Servicio centralizado para caché distribuido (Redis o Memory Cache)
    /// Proporciona métodos simples para Get/Set con TTL
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task RemoveAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
    }

    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        // Default TTLs por tipo de dato
        private static readonly TimeSpan DefaultBotTtl = TimeSpan.FromHours(1);
        private static readonly TimeSpan DefaultTemplateTtl = TimeSpan.FromHours(24);
        private static readonly TimeSpan DefaultUserTtl = TimeSpan.FromHours(1);

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Obtener valor del caché
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cachedValue = await _cache.GetAsync(key);
                if (cachedValue == null)
                {
                    return default;
                }

                var json = System.Text.Encoding.UTF8.GetString(cachedValue);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Cache GET error for key '{key}': {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Guardar valor en el caché con TTL
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                var cacheOptions = new DistributedCacheEntryOptions();
                if (ttl.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = ttl;
                }
                else
                {
                    // Default TTL: 1 hora
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                }

                await _cache.SetAsync(key, bytes, cacheOptions);
                _logger.LogInformation($"✅ Cache SET: key='{key}', ttl={ttl?.TotalMinutes ?? 60}m");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Cache SET error for key '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Remover valor del caché
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogInformation($"✅ Cache REMOVE: key='{key}'");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Cache REMOVE error for key '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get-or-Set pattern: Obtener del caché, si no existe generar con factory y guardar
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            try
            {
                // Intentar obtener del caché
                var cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    _logger.LogInformation($"✅ Cache HIT: key='{key}'");
                    return cached;
                }

                _logger.LogInformation($"⚠️ Cache MISS: key='{key}'");

                // Si no está en caché, generar con factory
                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, ttl);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Cache GET-OR-SET error for key '{key}': {ex.Message}");
                // Fallback: generar sin caché
                return await factory();
            }
        }
    }
}
