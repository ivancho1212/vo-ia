using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voia.Api.Models.Bots;

namespace Voia.Api.Services
{
    public class DataField
    {
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class PromptBuilderService
    {
        private readonly HttpClient _httpClient;

        public PromptBuilderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string BuildDataCaptureStatusPrompt(List<DataField> fields)
        {
            if (fields == null || fields.Count == 0)
                return string.Empty;

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
            status.AppendLine(
                $"DATOS CAPTURADOS: {(captured.Any() ? string.Join(", ", captured.Select(f => $"{f.FieldName}='{f.Value}'")) : "Ninguno")}."
            );
            status.AppendLine(
                $"DATOS PENDIENTES: {string.Join(", ", missing.Select(f => f.FieldName))}."
            );
            status.AppendLine(
                $"ACCIÓN: Pregunta únicamente por '{missing[0].FieldName}'. No repitas saludos ni confirmes datos anteriores."
            );
            status.AppendLine("-----------------------");

            return status.ToString();
        }

        /// <summary>
        /// Construye un prompt dinámico llamando al endpoint /api/Bots/{botId}/context
        /// </summary>
        public async Task<string> BuildPromptFromBotContextAsync(
            int botId,
            string userMessage,
            List<DataField> capturedFields
        )
        {
            // En modo desarrollo/demo, devolvemos un prompt simple que el mock entiende
            bool isMock = true; // 👈 Puedes usar una variable de configuración si quieres
            if (isMock)
            {
                return $"👤 Usuario dice: {userMessage}";
            }

            // Llamada al endpoint real
            var url = $"http://localhost:5006/api/Bots/{botId}/context";
            FullBotContextDto botContext;
            try
            {
                botContext = await _httpClient.GetFromJsonAsync<FullBotContextDto>(url)
                             ?? throw new Exception("No se recibió información del bot.");
            }
            catch (Exception ex)
            {
                return $"⚠️ No se pudo obtener contexto del bot: {ex.Message}\nUsuario dice: {userMessage}";
            }

            var sb = new StringBuilder();

            // 1️⃣ SystemPrompt
            if (!string.IsNullOrWhiteSpace(botContext.SystemPrompt))
                sb.AppendLine(botContext.SystemPrompt.Trim());

            // 2️⃣ Estado de captura
            var captureFieldsFromContext = botContext.Capture?.Fields?
                .Select(f => new DataField
                {
                    FieldName = f.Name,
                    Value = capturedFields?.FirstOrDefault(c => c.FieldName == f.Name)?.Value
                })
                .ToList() ?? new List<DataField>();

            sb.AppendLine();
            sb.AppendLine(BuildDataCaptureStatusPrompt(captureFieldsFromContext));

            // 3️⃣ Recursos del bot
            sb.AppendLine();
            sb.AppendLine("--- RECURSOS DEL BOT ---");
            if ((botContext.Documents?.Any() ?? false) ||
                (botContext.Urls?.Any() ?? false) ||
                (botContext.CustomTexts?.Any() ?? false))
            {
                if (botContext.Documents?.Any() ?? false)
                    sb.AppendLine("Documentos:\n" + string.Join("\n", botContext.Documents));

                if (botContext.Urls?.Any() ?? false)
                    sb.AppendLine("URLs:\n" + string.Join("\n", botContext.Urls));

                if (botContext.CustomTexts?.Any() ?? false)
                    sb.AppendLine("Textos personalizados:\n" + string.Join("\n", botContext.CustomTexts));
            }
            else
            {
                sb.AppendLine("Sin recursos adicionales.");
            }

            // 4️⃣ Historial de CustomPrompts
            sb.AppendLine();
            sb.AppendLine("🗨️ Conversación previa:");
            if (botContext.CustomPrompts?.Any() ?? false)
                sb.AppendLine(string.Join("\n", botContext.CustomPrompts.Select(m => $"{m.Role}: {m.Content}")));
            else
                sb.AppendLine("No hay historial previo.");

            // 5️⃣ Último mensaje del usuario
            sb.AppendLine();
            sb.AppendLine("👤 Usuario dice:");
            sb.AppendLine(userMessage);

            return sb.ToString();
        }

    }
}
