using System;
using System.IO;
using System.Threading.Tasks;

namespace Voia.Api.Services.Chat
{
    public interface IChatFileService
    {
        Task<string> SaveBase64FileAsync(string base64, string fileName);
    }

    public class ChatFileService : IChatFileService
    {
        public async Task<string> SaveBase64FileAsync(string base64, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName);
                var uniqueName = $"{Guid.NewGuid()}{extension}";
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
                Directory.CreateDirectory(uploadsPath);

                var path = Path.Combine(uploadsPath, uniqueName);
                byte[] fileBytes = Convert.FromBase64String(base64);
                await File.WriteAllBytesAsync(path, fileBytes);

                return $"/uploads/chat/{uniqueName}";
            }
            catch (Exception ex)
            {
                // Puedes lanzar una excepción custom o loguear si prefieres
                throw new IOException("Error al guardar el archivo base64", ex);
            }
        }
    }
}
