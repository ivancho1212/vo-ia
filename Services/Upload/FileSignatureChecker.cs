using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Voia.Api.Services.Upload
{
    public interface IFileSignatureChecker
    {
        /// <summary>
        /// Validates the file at <paramref name="filePath"/> against common magic numbers
        /// and basic policy. Returns the detected MIME type (e.g. "image/png", "application/pdf").
        /// Throws an InvalidDataException when the file is not allowed or cannot be detected.
        /// </summary>
        Task<string> ValidateAsync(string filePath, string declaredMime, string originalFileName);
    }

    public class FileSignatureChecker : IFileSignatureChecker
    {
        public async Task<string> ValidateAsync(string filePath, string declaredMime, string originalFileName)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found for validation", filePath);

            // Read the first bytes (up to 512) for detection
            byte[] buffer = new byte[512];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var read = await fs.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0) throw new InvalidDataException("Empty file");
            }

            // Helper lambdas
            bool StartsWith(byte[] src, byte[] pat)
            {
                if (src.Length < pat.Length) return false;
                for (int i = 0; i < pat.Length; i++) if (src[i] != pat[i]) return false;
                return true;
            }

            string AsAscii(int offset, int len)
            {
                try { return Encoding.ASCII.GetString(buffer, offset, len); } catch { return string.Empty; }
            }

            // Detect common types
            // PNG
            var pngSig = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            if (StartsWith(buffer, pngSig)) return "image/png";

            // JPEG (FF D8 FF)
            var jpgSig = new byte[] { 0xFF, 0xD8, 0xFF };
            if (StartsWith(buffer, jpgSig)) return "image/jpeg";

            // GIF
            var gif87 = Encoding.ASCII.GetBytes("GIF87a");
            var gif89 = Encoding.ASCII.GetBytes("GIF89a");
            if (StartsWith(buffer, gif87) || StartsWith(buffer, gif89)) return "image/gif";

            // WebP (RIFF....WEBP)
            var riff = Encoding.ASCII.GetBytes("RIFF");
            var webp = Encoding.ASCII.GetBytes("WEBP");
            if (StartsWith(buffer, riff) && AsAscii(8, 4) == "WEBP") return "image/webp";

            // PDF '%PDF-'
            if (AsAscii(0, 4) == "%PDF") return "application/pdf";

            // ZIP-based (docx/xlsx/pptx) - PK\x03\x04
            var pk = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            if (StartsWith(buffer, pk))
            {
                // Map declared extension heuristics
                var ext = Path.GetExtension(originalFileName ?? string.Empty).ToLowerInvariant();
                if (ext == ".docx" || ext == ".xlsx" || ext == ".pptx")
                    return "application/zip"; // caller can map to office types if needed
                return "application/zip";
            }

            // Plain text heuristics (first bytes printable)
            bool isText = true;
            int printable = 0;
            for (int i = 0; i < 128 && i < buffer.Length; i++)
            {
                var b = buffer[i];
                if (b == 0) { isText = false; break; }
                if (b >= 0x20 && b <= 0x7E) printable++;
            }
            if (isText && printable > 10) return "text/plain";

            // Dangerous signatures: EXE (MZ), ELF, scripts starting with #!
            var mz = Encoding.ASCII.GetBytes("MZ");
            if (StartsWith(buffer, mz)) throw new InvalidDataException("Executable files are not allowed.");

            if (buffer.Length >= 4 && buffer[0] == 0x7F && buffer[1] == (byte)'E' && buffer[2] == (byte)'L' && buffer[3] == (byte)'F')
                throw new InvalidDataException("Executable files are not allowed.");

            // Shebang (#!) scripts
            if (AsAscii(0, 2) == "#!") throw new InvalidDataException("Script files are not allowed.");

            // Fallback: check declared mime - allow some openness but be conservative
            if (!string.IsNullOrWhiteSpace(declaredMime))
            {
                var allowList = new[] { "image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf", "text/plain", "application/zip", "application/octet-stream" };
                if (Array.Exists(allowList, m => m.Equals(declaredMime, StringComparison.OrdinalIgnoreCase)))
                {
                    // If declared mime is allowed but we couldn't detect, accept as declared but log caution.
                    return declaredMime;
                }
            }

            throw new InvalidDataException("Unknown or disallowed file type.");
        }
    }
}
