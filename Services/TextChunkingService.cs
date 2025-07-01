// Services/TextChunkingService.cs
using System.Text.RegularExpressions;
using Voia.Api.Models;

namespace Voia.Api.Services
{
    public class TextChunkingService
    {
        public List<KnowledgeChunk> SplitTextIntoChunks(string text, string sourceType, int sourceId, int? sessionId = null, int? uploadedDocumentId = null)
        {
            var chunks = new List<KnowledgeChunk>();

            // Dividir por párrafos o cada 1000 caracteres como máximo
            var paragraphs = Regex.Split(text, @"\n\s*\n")
                                  .Where(p => !string.IsNullOrWhiteSpace(p))
                                  .ToList();

            foreach (var para in paragraphs)
            {
                var trimmed = para.Trim();
                if (trimmed.Length == 0) continue;

                // Opcional: dividir aún más si el párrafo es muy largo
                var subChunks = SplitIntoLengthChunks(trimmed, 1000);

                foreach (var chunkText in subChunks)
                {
                    var chunk = new KnowledgeChunk
                    {
                        UploadedDocumentId = uploadedDocumentId ?? 0,
                        Content = chunkText,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            source = sourceType,
                            source_id = sourceId
                        }),
                        TemplateTrainingSessionId = sessionId,
                        CreatedAt = DateTime.UtcNow
                    };

                    chunks.Add(chunk);
                }
            }

            return chunks;
        }

        private List<string> SplitIntoLengthChunks(string text, int maxLength)
        {
            var result = new List<string>();
            for (int i = 0; i < text.Length; i += maxLength)
            {
                result.Add(text.Substring(i, Math.Min(maxLength, text.Length - i)));
            }
            return result;
        }

    }
}
