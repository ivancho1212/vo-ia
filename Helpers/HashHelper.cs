using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Voia.Api.Helpers
{
    public static class HashHelper
    {
        public static string ComputeFileHash(IFormFile file)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = file.OpenReadStream())
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static string ComputeStringHash(string input)
        {
            // Normalizar la URL antes de generar el hash
            input = input.TrimEnd('/').ToLowerInvariant();
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
