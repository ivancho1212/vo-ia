using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Voia.Api.Middleware
{
    /// <summary>
    /// Middleware para validación adicional de CORS en endpoints críticos.
    /// Proporciona validación de origen más estricta que el middleware CORS estándar.
    /// Úsalo para:
    /// - Endpoints que manejan datos sensibles del usuario
    /// - Operaciones que modifican configuración de seguridad
    /// - Endpoints de administración
    /// 
    /// NOTA: Este middleware se ejecuta DESPUÉS del middleware CORS estándar.
    /// Si CORS estándar rechaza la solicitud, este middleware no se ejecutará.
    /// </summary>
    public class CorsOriginValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorsOriginValidationMiddleware> _logger;
        private readonly string[] _strictOrigins;

        /// <summary>
        /// Inicializa el middleware con lista de orígenes permitidos en modo estricto.
        /// </summary>
        public CorsOriginValidationMiddleware(RequestDelegate next, ILogger<CorsOriginValidationMiddleware> logger, IConfiguration config)
        {
            _next = next;
            _logger = logger;

            // Orígenes permitidos en modo estricto (producción)
            var strictOriginsConfig = config["CORS:StrictOrigins"] ?? 
                "https://voia-client.lat,https://app.voia.lat";
            
            _strictOrigins = strictOriginsConfig
                .Split(",")
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrEmpty(o))
                .ToArray();
        }

        /// <summary>
        /// Procesa la solicitud y valida el origen CORS en endpoints críticos.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // ✅ Permitir solicitudes sin origen (same-origin requests, server-side)
            if (!context.Request.Headers.ContainsKey("Origin"))
            {
                await _next(context);
                return;
            }

            var origin = context.Request.Headers["Origin"].ToString();

            // 📌 ENDPOINTS CRÍTICOS - Validación estricta
            if (IsStrictEndpoint(context.Request.Path))
            {
                if (!IsOriginAllowed(origin, _strictOrigins))
                {
                    _logger.LogWarning(
                        "🚨 CORS SECURITY: Rejected request from unauthorized origin '{Origin}' to strict endpoint '{Path}'",
                        origin,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Forbidden",
                        message = "Origin not allowed for this endpoint"
                    });
                    return;
                }

                _logger.LogInformation(
                    "✅ CORS SECURITY: Validated origin '{Origin}' for strict endpoint '{Path}'",
                    origin,
                    context.Request.Path);
            }

            await _next(context);
        }

        /// <summary>
        /// Determina si un endpoint requiere validación CORS estricta.
        /// </summary>
        private static bool IsStrictEndpoint(PathString path)
        {
            // Endpoints administrativos y de seguridad
            var strictPaths = new[]
            {
                "/api/users/security",           // Cambio de contraseña
                "/api/users/email",              // Cambio de email
                "/api/bots/full-rollback",       // Eliminación permanente de bots
                "/api/settings/security",        // Configuración de seguridad
                "/api/admin/",                   // Cualquier endpoint admin
            };

            var pathValue = path.Value ?? string.Empty;
            return strictPaths.Any(p => pathValue.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Valida si el origen está en la lista permitida.
        /// Incluye validación de HTTPS en producción.
        /// </summary>
        private static bool IsOriginAllowed(string origin, string[] allowedOrigins)
        {
            if (string.IsNullOrEmpty(origin))
                return false;

            // ✅ Comparación directa
            if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return true;

            // 🔒 SECURITY: En producción, rechazar orígenes no-HTTPS
            if (!origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Permitir localhost para desarrollo
                if (origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
                    origin.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Localhost no permitido en endpoints estrictos (usar env var para desarrollo)
                }
                return false;
            }

            return false;
        }
    }
}
