using System.Text;
using System.Linq;

namespace Voia.Api.Services
{
    public class DataField
    {
        public string FieldName { get; set; }
        public string Value { get; set; }
    }

    public class PromptBuilderService
    {
        public string BuildDataCaptureStatusPrompt(List<DataField> fields)
        {
            if (fields == null || fields.Count == 0) return "";

            var captured = fields.Where(f => !string.IsNullOrEmpty(f.Value)).ToList();
            var missing = fields.Where(f => string.IsNullOrEmpty(f.Value)).ToList();

            if (!missing.Any())
            {
                return @"--- GESTIÓN DE DATOS ---
✅ Todos los datos requeridos fueron capturados. Ahora responde normalmente.
-----------------------";
            }

            var status = new StringBuilder();
            status.AppendLine("--- GESTIÓN DE DATOS ---");
            status.AppendLine($"DATOS CAPTURADOS: {(captured.Any() ? string.Join(", ", captured.Select(f => $"{f.FieldName}='{f.Value}'")) : "Ninguno")}.");
            status.AppendLine($"DATOS PENDIENTES: {string.Join(", ", missing.Select(f => f.FieldName))}.");
            status.AppendLine($"ACCIÓN: Pregunta únicamente por '{missing[0].FieldName}'. No repitas saludos ni confirmes datos anteriores.");
            status.AppendLine("-----------------------");

            return status.ToString();
        }

        public string BuildDynamicPrompt(
            string systemMessage,
            string userMessage,
            string relevantContext,
            string conversationSummary,
            List<DataField> capturedFields
        )
        {
            var contextInfo = string.IsNullOrWhiteSpace(relevantContext) ? "Sin resultados relevantes en la base de conocimiento." : relevantContext.Trim();
            var historyInfo = string.IsNullOrWhiteSpace(conversationSummary) ? "No hay historial previo." : conversationSummary.Trim();

            return $@"
{systemMessage}

{BuildDataCaptureStatusPrompt(capturedFields)}

---
📚 Contexto (Qdrant/MySQL):
{contextInfo}

🗨️ Conversación previa:
{historyInfo}

👤 Usuario dice:
{userMessage}
";
        }
    }
}
