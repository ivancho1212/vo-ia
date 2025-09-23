using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using NPOI.HWPF;
using NPOI.HWPF.Extractor;

namespace Voia.Api.Services
{
    public class TextExtractionService
    {
        public string ExtractText(string filePath, string fileType)
        {
            var ext = Path.GetExtension(filePath).ToLower();

            try
            {
                if (ext == ".pdf" || fileType.Contains("pdf"))
                    return ExtractTextFromPdf(filePath);

                if (ext == ".docx" || fileType.Contains("docx") || fileType.Contains("msword"))
                    return ExtractTextFromDocx(filePath);

                if (ext == ".doc")
                    return ExtractTextFromDoc(filePath);

                if (ext == ".txt" || fileType.Contains("text") || fileType.Contains("plain"))
                    return File.ReadAllText(filePath);

                throw new NotSupportedException($"Tipo de archivo no soportado: {ext}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extrayendo texto: {ex.Message}");
            }
        }

        private string ExtractTextFromPdf(string filePath)
        {
            var sb = new StringBuilder();
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }

        private string ExtractTextFromDocx(string filePath)
        {
            var sb = new StringBuilder();
            using (var wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                    sb.Append(body.InnerText);
            }
            return sb.ToString();
        }

        private string ExtractTextFromDoc(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var doc = new HWPFDocument(stream);
            var extractor = new WordExtractor(doc);
            return extractor.Text;
        }
    }
}
