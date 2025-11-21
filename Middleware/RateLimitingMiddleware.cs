using System.Net;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace Voia.Api.Middleware
{
    /// <summary>
    /// Rate limiting middleware que controla el número de solicitudes por IP y por usuario autenticado.
    /// Usa Redis para almacenamiento distribuido (opcional) o memoria local.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitOptions _options;

        // In-memory fallback counters (cuando Redis no está disponible)
        private static readonly Dictionary<string, (int count, DateTime resetTime)> _inMemoryCounters = new();
        private static readonly object _lockObject = new();

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            RateLimitOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // No aplicar rate limiting a ciertos endpoints
            if (IsExcludedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Intentar obtener Redis de manera segura (puede ser null si no está configurado)
            var redis = serviceProvider.GetService(typeof(IConnectionMultiplexer)) as IConnectionMultiplexer;

            var key = GetRateLimitKey(context);
            var (allowed, remaining, resetTime) = await CheckRateLimit(key, redis);

            // Agregar headers de información
            context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString("o");

            if (!allowed)
            {
                _logger.LogWarning($"Rate limit exceeded for key: {key}");
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = Math.Ceiling((resetTime - DateTime.UtcNow).TotalSeconds).ToString();
                
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = resetTime
                });
                return;
            }

            await _next(context);
        }

        private string GetRateLimitKey(HttpContext context)
        {
            // Prioridad: Usuario autenticado > IP
            var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
            {
                return $"ratelimit:user:{userIdClaim.Value}";
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ratelimit:ip:{ip}";
        }

        private async Task<(bool allowed, int remaining, DateTime resetTime)> CheckRateLimit(string key, IConnectionMultiplexer? redis)
        {
            if (redis != null && redis.IsConnected)
            {
                return await CheckRateLimitRedis(key, redis);
            }
            else
            {
                return CheckRateLimitInMemory(key);
            }
        }

        private async Task<(bool allowed, int remaining, DateTime resetTime)> CheckRateLimitRedis(string key, IConnectionMultiplexer redis)
        {
            try
            {
                var db = redis.GetDatabase();
                var now = DateTime.UtcNow;
                var windowKey = key + ":window";
                var countKey = key + ":count";

                // Obtener timestamp de la ventana
                var windowValue = await db.StringGetAsync(windowKey);
                DateTime window;

                if (windowValue.IsNull)
                {
                    // Primera solicitud en esta ventana
                    window = now.AddMinutes(1);
                    await db.StringSetAsync(windowKey, window.Ticks.ToString(), TimeSpan.FromMinutes(1.5));
                }
                else
                {
                    window = new DateTime(long.Parse(windowValue!.ToString()));
                }

                // Si la ventana expiró, resetear
                if (now > window)
                {
                    await db.KeyDeleteAsync(new RedisKey[] { windowKey, countKey });
                    window = now.AddMinutes(1);
                    await db.StringSetAsync(windowKey, window.Ticks.ToString(), TimeSpan.FromMinutes(1.5));
                    var remaining = _options.RequestsPerMinute - 1;
                    await db.StringIncrementAsync(countKey, 1);
                    await db.KeyExpireAsync(countKey, TimeSpan.FromMinutes(1.5));
                    return (true, remaining, window);
                }

                // Incrementar contador
                var count = await db.StringIncrementAsync(countKey, 1);
                var remaining2 = Math.Max(0, _options.RequestsPerMinute - (int)count);

                return (count <= _options.RequestsPerMinute, remaining2, window);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Redis rate limit check failed: {ex.Message}. Falling back to in-memory.");
                return CheckRateLimitInMemory(key);
            }
        }

        private (bool allowed, int remaining, DateTime resetTime) CheckRateLimitInMemory(string key)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                if (_inMemoryCounters.TryGetValue(key, out var current))
                {
                    if (now > current.resetTime)
                    {
                        // Ventana expirada, resetear
                        _inMemoryCounters[key] = (1, now.AddMinutes(1));
                        return (true, _options.RequestsPerMinute - 1, now.AddMinutes(1));
                    }

                    if (current.count < _options.RequestsPerMinute)
                    {
                        _inMemoryCounters[key] = (current.count + 1, current.resetTime);
                        return (true, _options.RequestsPerMinute - current.count - 1, current.resetTime);
                    }

                    return (false, 0, current.resetTime);
                }

                // Primera solicitud
                _inMemoryCounters[key] = (1, now.AddMinutes(1));
                return (true, _options.RequestsPerMinute - 1, now.AddMinutes(1));
            }
        }

        private bool IsExcludedPath(PathString path)
        {
            var excludedPaths = new[]
            {
                "/swagger",
                "/health",
                "/auth/login",
                "/auth/register",
                "/auth/forgot-password",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/forgot-password"
            };

            return excludedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Opciones de configuración para Rate Limiting
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Número de solicitudes permitidas por minuto
        /// </summary>
        public int RequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// Habilitar rate limiting global
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Atributo para excluir endpoints específicos del rate limiting
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipRateLimitAttribute : Attribute
    {
    }
}
