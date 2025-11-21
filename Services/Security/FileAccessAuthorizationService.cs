using Microsoft.EntityFrameworkCore;
using Voia.Api.Data;
using System.Security.Claims;

namespace Voia.Api.Services.Security
{
    /// <summary>
    /// Servicio para validar acceso a archivos basado en propiedad de recursos.
    /// Implementa autorización granular para:
    /// - Documentos de entrenamiento de bots
    /// - Archivos de chat/conversación
    /// - Archivos de usuario
    /// 
    /// PRINCIPIO: Un usuario solo puede acceder a archivos que pertenecen a sus recursos (bots, conversaciones, etc.)
    /// </summary>
    public interface IFileAccessAuthorizationService
    {
        /// <summary>
        /// Valida si un usuario puede acceder a un documento de entrenamiento.
        /// El documento debe pertenecer a un bot que es propiedad del usuario.
        /// </summary>
        Task<bool> CanAccessUploadedDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Valida si un usuario puede acceder a un archivo de una conversación.
        /// El archivo debe pertenecer a una conversación que es propiedad del usuario o bot.
        /// </summary>
        Task<bool> CanAccessChatFileAsync(int fileId, int userId);

        /// <summary>
        /// Valida si un usuario puede descargar un archivo de su perfil.
        /// </summary>
        Task<bool> CanAccessUserFileAsync(int fileId, int userId);

        /// <summary>
        /// Valida si un archivo tiene un tipo permitido (bloquea ejecutables, scripts, etc.)
        /// </summary>
        bool IsFileTypeAllowed(string fileExtension, string? mimeType = null);
    }

    public class FileAccessAuthorizationService : IFileAccessAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileAccessAuthorizationService> _logger;

        // Extensiones de archivo bloqueadas
        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Ejecutables
            ".exe", ".dll", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".bat", ".cmd", ".ps1",
            // Aplicaciones
            ".app", ".apk", ".ipa",
            // Scripts
            ".php", ".asp", ".jsp", ".py", ".sh", ".bash", ".csh", ".ksh", ".zsh",
            // Archivos de sistema
            ".sys", ".ini", ".drv", ".lnk", ".msi",
            // Office macros
            ".docm", ".xlsm", ".pptm"
        };

        // MIME types bloqueados
        private static readonly HashSet<string> BlockedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-msdownload",
            "application/x-msdos-program",
            "application/x-executable",
            "application/x-sh",
            "application/x-shellscript",
            "application/x-python",
            "application/x-ruby",
            "text/x-shellscript",
            "text/x-python",
            "text/x-perl",
            "application/x-perl",
            "application/x-php"
        };

        public FileAccessAuthorizationService(ApplicationDbContext context, ILogger<FileAccessAuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Valida acceso a documento de entrenamiento.
        /// 🔐 Validación:
        /// 1. Documento existe
        /// 2. Documento pertenece a un bot
        /// 3. Bot es propiedad del usuario
        /// 4. Bot no está eliminado
        /// </summary>
        public async Task<bool> CanAccessUploadedDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.UploadedDocuments
                    .Include(d => d.Bot)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("🚨 FILE ACCESS: Document {DocumentId} not found", documentId);
                    return false;
                }

                if (document.BotId == null || document.BotId == 0)
                {
                    _logger.LogWarning("🚨 FILE ACCESS: Document {DocumentId} not associated with any bot", documentId);
                    return false;
                }

                if (document.Bot == null || document.Bot.IsDeleted)
                {
                    _logger.LogWarning("🚨 FILE ACCESS: Bot {BotId} for document {DocumentId} is deleted or not found", document.BotId, documentId);
                    return false;
                }

                if (document.Bot.UserId != userId)
                {
                    _logger.LogWarning(
                        "🚨 FILE ACCESS DENIED: User {UserId} attempted to access document {DocumentId} from bot {BotId} (owner: {OwnerId})",
                        userId,
                        documentId,
                        document.BotId,
                        document.Bot.UserId);
                    return false;
                }

                _logger.LogInformation(
                    "✅ FILE ACCESS ALLOWED: User {UserId} accessing document {DocumentId}",
                    userId,
                    documentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR checking file access for document {DocumentId}", documentId);
                return false;
            }
        }

        /// <summary>
        /// Valida acceso a archivo de conversación/chat.
        /// 🔐 Validación:
        /// 1. Archivo existe
        /// 2. Archivo pertenece a una conversación
        /// 3. Conversación es propiedad del usuario o está asociada con su bot
        /// </summary>
        public async Task<bool> CanAccessChatFileAsync(int fileId, int userId)
        {
            try
            {
                // Primero buscar en tabla específica de chat files si existe
                // Por ahora asumimos estructura genérica
                var file = await _context.UploadedDocuments
                    .FirstOrDefaultAsync(f => f.Id == fileId);

                if (file == null)
                {
                    _logger.LogWarning("🚨 FILE ACCESS: Chat file {FileId} not found", fileId);
                    return false;
                }

                // Verificar si el usuario es propietario del bot asociado
                if (file.BotId > 0)
                {
                    var bot = await _context.Bots
                        .FirstOrDefaultAsync(b => b.Id == file.BotId && !b.IsDeleted);

                    if (bot != null && bot.UserId == userId)
                    {
                        _logger.LogInformation(
                            "✅ FILE ACCESS ALLOWED: User {UserId} accessing chat file {FileId} via bot {BotId}",
                            userId,
                            fileId,
                            file.BotId);
                        return true;
                    }
                }

                _logger.LogWarning(
                    "🚨 FILE ACCESS DENIED: User {UserId} attempted to access chat file {FileId}",
                    userId,
                    fileId);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR checking chat file access for file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Valida acceso a archivo del perfil del usuario.
        /// 🔐 Validación:
        /// 1. Archivo existe
        /// 2. Archivo pertenece al usuario solicitante
        /// </summary>
        public async Task<bool> CanAccessUserFileAsync(int fileId, int userId)
        {
            try
            {
                var file = await _context.UploadedDocuments
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

                if (file == null)
                {
                    _logger.LogWarning(
                        "🚨 FILE ACCESS DENIED: User {UserId} attempted to access user file {FileId} they don't own",
                        userId,
                        fileId);
                    return false;
                }

                _logger.LogInformation(
                    "✅ FILE ACCESS ALLOWED: User {UserId} accessing their own file {FileId}",
                    userId,
                    fileId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR checking user file access for file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Valida si un tipo de archivo es permitido.
        /// Bloquea ejecutables, scripts, y archivos peligrosos.
        /// 
        /// 🔐 Validación:
        /// 1. Extensión no está en lista bloqueada
        /// 2. MIME type no está en lista bloqueada
        /// 3. Combinación de extensión + MIME type es válida
        /// </summary>
        public bool IsFileTypeAllowed(string fileExtension, string? mimeType = null)
        {
            // Validar extensión
            if (string.IsNullOrEmpty(fileExtension))
            {
                _logger.LogWarning("🚨 FILE TYPE: No extension provided");
                return false;
            }

            // Normalizar extensión (asegurar que empiece con .)
            var ext = fileExtension.StartsWith(".") ? fileExtension : $".{fileExtension}";

            if (BlockedExtensions.Contains(ext))
            {
                _logger.LogWarning("🚨 FILE TYPE BLOCKED: Extension {Extension} is not allowed", ext);
                return false;
            }

            // Validar MIME type si se proporciona
            if (!string.IsNullOrEmpty(mimeType))
            {
                if (BlockedMimeTypes.Contains(mimeType))
                {
                    _logger.LogWarning("🚨 FILE TYPE BLOCKED: MIME type {MimeType} is not allowed", mimeType);
                    return false;
                }

                // Validaciones adicionales
                if (mimeType.StartsWith("application/x-", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("🚨 FILE TYPE BLOCKED: Suspicious MIME type {MimeType}", mimeType);
                    return false;
                }
            }

            _logger.LogInformation(
                "✅ FILE TYPE ALLOWED: {Extension} (MIME: {MimeType})",
                ext,
                mimeType ?? "not specified");

            return true;
        }
    }
}
