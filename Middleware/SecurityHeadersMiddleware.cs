using System;
using Microsoft.AspNetCore.Http;

namespace Voia.Api.Middleware
{
    /// <summary>
    /// Security Headers Middleware que agrega headers de seguridad a todas las respuestas.
    /// Protege contra:
    /// - Clickjacking (X-Frame-Options)
    /// - MIME type sniffing (X-Content-Type-Options)
    /// - XSS attacks (X-XSS-Protection, CSP)
    /// - Downgrade attacks (HSTS)
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Agregar security headers a la respuesta
            AddSecurityHeaders(context.Response);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpResponse response)
        {
            // 1. X-Frame-Options: DENY
            // Previene clickjacking - el sitio no puede ser embebido en iframes
            response.Headers["X-Frame-Options"] = "DENY";

            // 2. X-Content-Type-Options: nosniff
            // Previene MIME type sniffing - el navegador debe respetar el Content-Type
            response.Headers["X-Content-Type-Options"] = "nosniff";

            // 3. X-XSS-Protection: 1; mode=block
            // Habilita protección XSS en navegadores antiguos
            response.Headers["X-XSS-Protection"] = "1; mode=block";

            // 4. Referrer-Policy: strict-origin-when-cross-origin
            // Controla qué información de referrer se envía
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 5. Permissions-Policy (antes Feature-Policy)
            // Controla qué APIs del navegador se pueden usar
            response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";

            // 6. Content-Security-Policy (CSP)
            // Política de seguridad completa para prevenir inyecciones de contenido
            var csp = BuildContentSecurityPolicy();
            response.Headers["Content-Security-Policy"] = csp;

            // 7. Strict-Transport-Security (HSTS)
            // Fuerza HTTPS en todas las comunicaciones futuras
            // max-age=31536000 = 1 año en segundos
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            _logger.LogDebug("Security headers agregados a la respuesta");
        }

        private string BuildContentSecurityPolicy()
        {
            // CSP completa pero balanceada entre seguridad y funcionalidad
            var policies = new[]
            {
                // default-src 'none' - denegar todo por defecto
                "default-src 'none'",

                // script-src - solo scripts de nuestra origem, sin inline scripts
                "script-src 'self' https://cdn.jsdelivr.net",

                // style-src - estilos de nuestra origen y CDN, permitir unsafe-inline para compatibilidad
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com",

                // img-src - imágenes de nuestra origen, data URLs, y https
                "img-src 'self' data: https:",

                // font-src - fuentes de nuestra origen y Google Fonts
                "font-src 'self' https://fonts.gstatic.com https://fonts.googleapis.com",

                // connect-src - conexiones AJAX/WebSocket a nuestra origen y APIs
                "connect-src 'self' https: wss: ws:",

                // form-action - solo enviar formularios a nuestra origen
                "form-action 'self'",

                // frame-ancestors - no permitir embebimiento en iframes (redundante con X-Frame-Options)
                "frame-ancestors 'none'",

                // base-uri - solo permitir <base> de nuestra origen
                "base-uri 'self'",

                // object-src - no permitir <object>, <embed>, <applet>
                "object-src 'none'",

                // media-src - media de nuestra origen
                "media-src 'self'",

                // upgrade-insecure-requests - actualizar automáticamente http a https
                "upgrade-insecure-requests",

                // block-all-mixed-content - bloquear contenido http en https
                "block-all-mixed-content"
            };

            return string.Join("; ", policies);
        }
    }

    /// <summary>
    /// Opciones de configuración para Security Headers
    /// </summary>
    public class SecurityHeadersOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Si true, usa CSP en modo report-only (solo reporta violaciones, no bloquea)
        /// Útil para testing antes de enforce
        /// </summary>
        public bool ReportOnly { get; set; } = false;

        /// <summary>
        /// Endpoint para reportar violaciones de CSP
        /// </summary>
        public string ReportUri { get; set; } = "/api/security/csp-report";

        /// <summary>
        /// Habilitar HSTS (Strict-Transport-Security)
        /// </summary>
        public bool EnableHSTS { get; set; } = true;

        /// <summary>
        /// Edad máxima de HSTS en segundos (default: 1 año)
        /// </summary>
        public int HSTSMaxAge { get; set; } = 31536000;

        /// <summary>
        /// Habilitar preload de HSTS (agregar al preload list)
        /// </summary>
        public bool HSTSPreload { get; set; } = true;

        /// <summary>
        /// Incluir subdomains en HSTS
        /// </summary>
        public bool HSTSIncludeSubDomains { get; set; } = true;
    }
}
