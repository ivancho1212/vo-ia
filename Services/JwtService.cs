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
            string normalizedRole = user.Role.Name switch
            {
                "Administrador" or "Admin" => "Admin",
                "Usuario" or "User" => "User",
                "Soporte" or "Support" => "Support",
                "Entrenador" or "Trainer" => "Trainer",
                "Espectador" or "Viewer" => "Viewer",
                _ => user.Role.Name
            };


            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, normalizedRole)
            };

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
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
