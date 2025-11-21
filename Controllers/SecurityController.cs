using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityController : ControllerBase
    {
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(ILogger<SecurityController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para reportar violaciones de Content-Security-Policy
        /// El navegador envía automáticamente violaciones aquí cuando CSP-Report-Only está configurado
        /// </summary>
        [HttpPost("csp-report")]
        [AllowAnonymous]
        public async Task<IActionResult> ReportCspViolation()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(body))
                {
                    return Ok();
                }

                var violation = JsonSerializer.Deserialize<CspViolationReport>(body);

                if (violation != null)
                {
                    _logger.LogWarning(
                        $"CSP Violation Reported: " +
                        $"DocumentUri={violation.DocumentUri}, " +
                        $"ViolatedDirective={violation.ViolatedDirective}, " +
                        $"BlockedUri={violation.BlockedUri}, " +
                        $"OriginalPolicy={violation.OriginalPolicy}");

                    // Aquí podrías enviar a un servicio de monitoreo (Sentry, etc)
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing CSP violation report: {ex.Message}");
                return Ok(); // Siempre retornar OK para no causar errores en el cliente
            }
        }

        /// <summary>
        /// GET endpoint para obtener el estado de seguridad del servidor
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetSecurityStatus()
        {
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = "OK",
                features = new
                {
                    rateLimiting = true,
                    csrfProtection = true,
                    auditTrail = true,
                    securityHeaders = true,
                    httpOnlyCookies = true
                }
            });
        }
    }

    /// <summary>
    /// Modelo para reportes de violaciones CSP
    /// Estructura según spec de Content-Security-Policy
    /// </summary>
    public class CspViolationReport
    {
        [System.Text.Json.Serialization.JsonPropertyName("document-uri")]
        public string DocumentUri { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("violated-directive")]
        public string ViolatedDirective { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("original-policy")]
        public string OriginalPolicy { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("blocked-uri")]
        public string BlockedUri { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("source-file")]
        public string SourceFile { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("line-number")]
        public int LineNumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("column-number")]
        public int ColumnNumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("disposition")]
        public string Disposition { get; set; }
    }
}
