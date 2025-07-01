using System.IO.Compression;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;

namespace Voia.Api.Services
{
    public class TextExtractionService
    {
        public string ExtractText(string filePath, string fileType)
        {
            if (fileType.Contains("pdf"))
            {
                return ExtractTextFromPdf(filePath);
            }

            if (fileType.Contains("msword") || fileType.Contains("docx"))
            {
                return ExtractTextFromDocx(filePath);
            }

            if (fileType.Contains("text") || fileType.Contains("plain"))
            {
                return File.ReadAllText(filePath);
            }

            return "";
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
                {
                    sb.Append(body.InnerText);
                }
            }

            return sb.ToString();
        }
    }
}
