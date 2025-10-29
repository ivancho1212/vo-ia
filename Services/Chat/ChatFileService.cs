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
        private readonly Voia.Api.Services.Upload.IFileSignatureChecker _checker;

        public ChatFileService(Voia.Api.Services.Upload.IFileSignatureChecker checker)
        {
            _checker = checker;
        }

        public async Task<string> SaveBase64FileAsync(string base64, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName);
                var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "tmp");
                Directory.CreateDirectory(tmpDir);

                var tmpName = $"{Guid.NewGuid()}{extension}";
                var tmpPath = Path.Combine(tmpDir, tmpName);

                byte[] fileBytes = Convert.FromBase64String(base64);
                await File.WriteAllBytesAsync(tmpPath, fileBytes);

                // Validate signature
                var detectedMime = await _checker.ValidateAsync(tmpPath, "", fileName);

                // Move to final folder
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
                Directory.CreateDirectory(uploadsPath);
                var finalName = $"{Guid.NewGuid()}{extension}";
                var finalPath = Path.Combine(uploadsPath, finalName);
                System.IO.File.Move(tmpPath, finalPath);

                return $"/uploads/chat/{finalName}";
            }
            catch (InvalidDataException ide)
            {
                throw new IOException("Validation failed for base64 file: " + ide.Message, ide);
            }
            catch (Exception ex)
            {
                // Puedes lanzar una excepción custom o loguear si prefieres
                throw new IOException("Error al guardar el archivo base64", ex);
            }
        }
    }
}
