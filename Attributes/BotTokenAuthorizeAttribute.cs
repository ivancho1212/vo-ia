using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;

namespace Voia.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BotTokenAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // ✅ Permitir requests OPTIONS para CORS preflight
            if (context.HttpContext.Request.Method == "OPTIONS")
            {
                return;
            }

            // Verificar si el endpoint tiene [AllowAnonymous]
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

            if (allowAnonymous)
            {
                return; // Saltar validación si permite anónimos
            }

            var request = context.HttpContext.Request;
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            // 1. Obtener token del header (Authorization: Bearer ...) o query string (?token=)
            var token = request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                token = request.Query["token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Authorization token is missing." });
                return;
            }

            var tokenKey = config["Jwt:Key"];
            if (string.IsNullOrEmpty(tokenKey))
            {
                context.Result = new StatusCodeResult(500); // Configuración inválida
                return;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // 2. Validar el token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // 3. Extraer claims
                var botIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "botId")?.Value;
                var allowedDomainClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "allowedDomain")?.Value;

                // Permitir tokens sin botId/allowedDomain SOLO en desarrollo
                if (string.IsNullOrEmpty(botIdClaim) || string.IsNullOrEmpty(allowedDomainClaim))
                {
                    if (!env.IsDevelopment())
                    {
                        context.Result = new ForbidResult(); // Token malformado
                        return;
                    }
                }

                // 4. Validar que el token siga existiendo en la base de datos (no revocado)
                var db = context.HttpContext.RequestServices.GetRequiredService<Voia.Api.Data.ApplicationDbContext>();
                var integration = db.BotIntegrations.FirstOrDefault(i => i.BotId.ToString() == botIdClaim && i.ApiTokenHash == token);
                if (integration == null)
                {
                    context.Result = new UnauthorizedObjectResult(new { error = "Token is revoked or integration deleted." });
                    return;
                }

                // 5. Validar dominio del request solo si no estamos en desarrollo
                if (!env.IsDevelopment())
                {
                    var requestOrigin = request.Headers["Origin"].FirstOrDefault() 
                                        ?? request.Headers["Referer"].FirstOrDefault();

                    if (string.IsNullOrEmpty(requestOrigin) || !IsDomainAllowed(requestOrigin, allowedDomainClaim))
                    {
                        context.Result = new ForbidResult(); // Dominio no permitido
                        return;
                    }
                }

                // 6. Validar que el botId del token coincide con el de la ruta (si aplica)
                if (context.RouteData.Values.TryGetValue("botId", out var routeBotId))
                {
                    if (!string.Equals(routeBotId?.ToString(), botIdClaim, StringComparison.Ordinal))
                    {
                        context.Result = new ForbidResult(); // Conflicto BotId
                        return;
                    }
                }

                // 7. Para endpoints sin botId en ruta, almacenar botId del token en HttpContext
                context.HttpContext.Items["TokenBotId"] = botIdClaim;
                context.HttpContext.Items["TokenAllowedDomain"] = allowedDomainClaim;
            }
            catch (SecurityTokenExpiredException)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Token has expired." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotTokenAuthorize] Error: {ex.Message}");
                context.Result = new ForbidResult(); // Cualquier otro error
            }
        }

    private bool IsDomainAllowed(string requestOrigin, string allowedDomain)
    {
        try
        {
            var requestUri = new Uri(requestOrigin);
            return requestUri.Host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase)
                   || requestUri.Host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
}
