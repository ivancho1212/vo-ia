using Voia.Api.Services;

namespace Voia.Api.Middleware
{
    /// <summary>
    /// Middleware para validar CSRF tokens en requests POST, PUT, PATCH, DELETE
    /// </summary>
    public class CsrfProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CsrfProtectionMiddleware> _logger;

        // Métodos HTTP que requieren CSRF token
        private static readonly HashSet<string> ProtectedMethods = new() { "POST", "PUT", "PATCH", "DELETE" };

        // Rutas que están excluidas de protección CSRF
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/forgot-password",
            "/api/auth/reset-password",
            "/auth/login",
            "/auth/register",
            "/swagger",
            "/health",
            "/api/bots/widget-session",  // Widget session creation
            "/api/conversations/start",   // Widget start conversation
            "/api/conversations/send",    // Widget send message
            "/api/conversations/get-or-create",  // Widget create conversation (public)
            "/api/botintegrations/generate-widget-token",  // Widget token generation (public)
            "/chatHub/negotiate", // Excluir negociación SignalR de CSRF
        };

        public CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICsrfTokenService csrfService)
        {
            var request = context.Request;

            // ✅ Skip OPTIONS requests (CORS preflight) - CRITICAL for CORS
            if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }


            // Excluir todas las rutas /api/* de la protección CSRF
            if (!ProtectedMethods.Contains(request.Method) || IsExcludedPath(request.Path) || request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            // Obtener token del header
            var tokenFromHeader = request.Headers[csrfService.GetHeaderName()].FirstOrDefault();

            // Para widgets, permitir token en query string o header
            if (IsWidgetRequest(request.Path) && string.IsNullOrEmpty(tokenFromHeader))
            {
                tokenFromHeader = request.Query[csrfService.GetHeaderName()].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(tokenFromHeader))
            {
                _logger.LogWarning($"CSRF token faltante en {request.Method} {request.Path}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "CSRF token missing",
                    message = "El token CSRF es requerido para esta operación",
                    header_name = csrfService.GetHeaderName()
                });
                return;
            }

            // Validar token
            if (!csrfService.ValidateToken(tokenFromHeader))
            {
                _logger.LogWarning($"CSRF token inválido en {request.Method} {request.Path}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid CSRF token",
                    message = "El token CSRF es inválido o ha expirado"
                });
                return;
            }

            await _next(context);
        }

        private bool IsExcludedPath(PathString path)
        {
            var pathString = path.Value ?? "";
            return ExcludedPaths.Any(excluded => pathString.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsWidgetRequest(PathString path)
        {
            var pathString = path.Value ?? "";
            return pathString.Contains("widget", StringComparison.OrdinalIgnoreCase) ||
                   pathString.Contains("conversations", StringComparison.OrdinalIgnoreCase);
        }
    }
}
