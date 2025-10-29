using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Voia.Api.Data;
using Voia.Api.Services.Upload;
using System.IO;

namespace Voia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileSignatureChecker _checker;

        public FilesController(ApplicationDbContext context, IFileSignatureChecker checker)
        {
            _context = context;
            _checker = checker;
        }

        // GET /api/files/chat/{id}
        // Dev: allowed anonymous to enable widget previews. In production consider requiring auth/ signed urls.
        [HttpGet("chat/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChatFile(int id)
        {
            var fileRec = await _context.ChatUploadedFiles.FirstOrDefaultAsync(f => f.Id == id);
            if (fileRec == null) return NotFound();

            var filePath = fileRec.FilePath ?? string.Empty; // e.g. /uploads/chat/xxxxx.png
            if (string.IsNullOrWhiteSpace(filePath)) return BadRequest(new { message = "Invalid file path" });

            // Map to physical storage under 'Uploads/chat' regardless of the stored filePath string.
            var storedFileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(storedFileName)) return NotFound();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "chat", storedFileName);
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            // Re-validate signature and detect mime
            string detectedMime;
            try
            {
                detectedMime = await _checker.ValidateAsync(physicalPath, fileRec.FileType ?? string.Empty, fileRec.FileName ?? string.Empty);
            }
            catch (InvalidDataException)
            {
                // If signature no longer valid (suspicious), return 403
                return StatusCode(403, new { message = "File is not allowed or failed validation." });
            }

            // Set safe headers
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            var isImage = detectedMime.StartsWith("image/");
            var disposition = isImage ? "inline" : "attachment";
            var fileName = Path.GetFileName(fileRec.FileName ?? "file");
            Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{fileName}\"";

            var fs = System.IO.File.OpenRead(physicalPath);
            return File(fs, detectedMime);
        }
    }
}
