using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Voia.Api.Models;

namespace Voia.Api.Services
{
    // La clase debe ser pública para que se pueda acceder desde otros lugares
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        // Método público para generar el token
        public string GenerateToken(User user)
        {
            // Obtener roles usando Identity (debe ser pasado como parámetro)
            // Ejemplo: el controlador debe obtener el rol con UserManager y pasarlo aquí

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Agregar el rol si está presente
            if (user.Role != null && !string.IsNullOrWhiteSpace(user.Role.Name))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
            }

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            // Leer minutos de expiración desde configuración, default 15 si no existe
            int expiryMinutes = 15;
            var expiryConfig = _config["Jwt:AccessTokenExpiryMinutes"];
            if (!string.IsNullOrWhiteSpace(expiryConfig) && int.TryParse(expiryConfig, out int configMinutes))
            {
                expiryMinutes = configMinutes;
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateWidgetToken(int botId, string allowedDomain)
        {
            var claims = new[]
            {
                new Claim("botId", botId.ToString()),
                new Claim("allowedDomain", allowedDomain)
            };

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddYears(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
