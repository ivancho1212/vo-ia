using Serilog.Context;

namespace Voia.Api.Middleware
{
    /// <summary>
    /// Middleware para loguear información de requests y responses
    /// Agrega contexto con RequestId, UserId, y duración
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generar RequestId único
            var requestId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            var userId = context.User?.FindFirst("sub")?.Value ?? "Anonymous";
            var startTime = DateTime.UtcNow;

            // Agregar propiedades al contexto de Serilog
            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("Path", context.Request.Path.Value))
            using (LogContext.PushProperty("Method", context.Request.Method))
            {
                try
                {
                    // Log de entrada
                    _logger.LogInformation(
                        "HTTP {Method} {Path} started - IP: {RemoteIP}",
                        context.Request.Method,
                        context.Request.Path.Value,
                        context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

                    // Llamar al siguiente middleware
                    await _next(context);

                    // Calcular duración
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Log de salida exitosa
                    _logger.LogInformation(
                        "HTTP {Method} {Path} completed - Status: {StatusCode} - Duration: {DurationMs}ms",
                        context.Request.Method,
                        context.Request.Path.Value,
                        context.Response.StatusCode,
                        duration);

                    // Log de warning si la solicitud fue lenta
                    if (duration > 5000)
                    {
                        _logger.LogWarning(
                            "Slow HTTP request detected: {Method} {Path} - {DurationMs}ms",
                            context.Request.Method,
                            context.Request.Path.Value,
                            duration);
                    }
                }
                catch (Exception ex)
                {
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    _logger.LogError(
                        ex,
                        "HTTP {Method} {Path} threw exception after {DurationMs}ms",
                        context.Request.Method,
                        context.Request.Path.Value,
                        duration);

                    throw;
                }
            }
        }
    }
}
