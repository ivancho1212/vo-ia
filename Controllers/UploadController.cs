using Microsoft.AspNetCore.Mvc;
using Voia.Api.Helpers;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Sube un archivo de avatar o imagen de estilo
        /// </summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [HasPermission("CanUploadFiles")]
    public async Task<IActionResult> UploadAvatar(IFormFile file, string type = "avatar")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No se recibió un archivo válido." });

                // Validar tipo de archivo
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Tipo de archivo no válido. Use JPG, PNG, GIF o WebP." });
                }

                // Validar tamaño (5MB máximo)
                const long maxSize = 5 * 1024 * 1024;
                if (file.Length > maxSize)
                {
                    return BadRequest(new { message = "El archivo es demasiado grande. Máximo 5MB." });
                }

                // Generar nombre único con hash
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(fileName);
                var contentHash = HashHelper.ComputeFileHash(file);
                var uniqueFileName = $"{contentHash}{fileExtension}";

                // Obtener ruta base del proyecto
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                while (!System.IO.File.Exists(Path.Combine(basePath, "Voia.Api.csproj")))
                {
                    basePath = Path.GetDirectoryName(basePath);
                    if (string.IsNullOrEmpty(basePath)) 
                        throw new Exception("No se pudo encontrar la raíz del proyecto");
                }

                // Crear carpeta de uploads si no existe
                var uploadsFolder = Path.Combine(basePath, "Uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Verificar si el archivo ya existe (por hash)
                if (System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation($"Archivo ya existe con hash {contentHash}, reutilizando: {uniqueFileName}");
                    return Ok(new { url = $"/uploads/{uniqueFileName}" });
                }

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"Avatar subido correctamente: {uniqueFileName} (tipo: {type})");

                return Ok(new { url = $"/uploads/{uniqueFileName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo avatar");
                return StatusCode(500, new { message = "Error interno del servidor al subir el archivo." });
            }
        }

        /// <summary>
        /// Elimina un archivo de upload
        /// </summary>
    [HttpDelete]
    [HasPermission("CanDeleteUploads")]
    public IActionResult DeleteFile([FromBody] DeleteFileRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Url))
                    return BadRequest(new { message = "URL del archivo requerida." });

                // Extraer nombre del archivo de la URL
                var fileName = Path.GetFileName(request.Url);
                
                // Obtener ruta base del proyecto
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                while (!System.IO.File.Exists(Path.Combine(basePath, "Voia.Api.csproj")))
                {
                    basePath = Path.GetDirectoryName(basePath);
                    if (string.IsNullOrEmpty(basePath)) 
                        throw new Exception("No se pudo encontrar la raíz del proyecto");
                }

                var filePath = Path.Combine(basePath, "Uploads", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Archivo eliminado: {fileName}");
                    return Ok(new { message = "Archivo eliminado correctamente." });
                }

                return NotFound(new { message = "Archivo no encontrado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando archivo");
                return StatusCode(500, new { message = "Error interno del servidor al eliminar el archivo." });
            }
        }
    }

    public class DeleteFileRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}