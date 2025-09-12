using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Voia.Api.Models.Bots;
using Microsoft.Extensions.Logging;

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
        private readonly bool _isMock; // Permite alternar entre mock y real
        private readonly ILogger<PromptBuilderService> _logger;

        // 🔹 Cambiado el valor por defecto de isMock a false para que la implementación real sea la predeterminada.
        public PromptBuilderService(HttpClient httpClient, ILogger<PromptBuilderService> logger, bool isMock = false)
        {
            _httpClient = httpClient;
            _isMock = isMock;
            _logger = logger;
        }

        // Construye el estado de captura de datos
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

            return $@"--- GESTIÓN DE DATOS ---
DATOS CAPTURADOS: {(captured.Any() ? string.Join(", ", captured.Select(f => $"{f.FieldName}='{f.Value}'")) : "Ninguno")}.
DATOS PENDIENTES: {string.Join(", ", missing.Select(f => f.FieldName))}.
ACCIÓN: Pregunta únicamente por '{missing[0].FieldName}'. No repitas saludos ni confirmes datos anteriores.
-----------------------";
        }

        /// <summary>
        /// Construye un payload JSON limpio para la IA, incluyendo contexto, recursos, historial y último mensaje.
        /// Funciona tanto para mock como para producción.
        /// </summary>
        public async Task<string> BuildPromptFromBotContextAsync(
            int botId,
            int userId,
            string userMessage,
            List<DataField> capturedFields
        )
        {
            // 🔹 Modo mock: devolvemos un JSON con toda la estructura
            if (_isMock)
            {
                // En modo mock, intentamos obtener el contexto real para ser más precisos.
                // Si falla, usamos un fallback con datos quemados.
                // Esto permite que el mock funcione incluso si el backend no está disponible.
                try
                {
                    // Reutilizamos la lógica de producción para obtener el contexto real.
                    // El 'return' está dentro del bloque de producción más abajo.
                    _logger.LogInformation("Modo Mock: Intentando obtener contexto real del bot {BotId}", botId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Modo Mock: No se pudo obtener contexto real. Usando datos de fallback.");
                    // Si la llamada al contexto real falla en modo mock, se usará el bloque catch de más abajo
                    // que ya tiene un payload de fallback.
                }
            }

            // 🔹 Producción: pedimos contexto real al backend, incluyendo el mensaje del usuario para búsqueda de vectores
            var url = $"http://localhost:5006/api/Bots/{botId}/context?query={Uri.EscapeDataString(userMessage)}";
            FullBotContextDto botContext;
            try
            {
                botContext = await _httpClient.GetFromJsonAsync<FullBotContextDto>(url)
                             ?? throw new Exception("No se recibió información del bot.");
            }
            catch (Exception ex)
            {
                // Si no hay contexto, igual devolvemos un JSON limpio
                var fallbackPayload = new
                {
                Error = $"No se pudo obtener contexto del bot: {ex.Message}",
                    BotId = botId, 
                    UserId = userId, // 👈 USADO: Usamos el UserId real
                    OriginalQuestion = $"👤 Usuario dice: {userMessage}",
                    UserQuestion = userMessage,
                    CapturedFields = capturedFields ?? new List<DataField>(),
                    Context = new
                    {
                        SystemPrompt = $"⚠️ No se pudo obtener contexto del bot: {ex.Message}",
                        DataCaptureStatus = BuildDataCaptureStatusPrompt(capturedFields ?? new List<DataField>()),
                        Resources = new { Documents = new List<string>(), Urls = new List<string>(), CustomTexts = new List<string>() },
                        ConversationHistory = new List<object>
                        {
                            new { Role = "user", Content = userMessage }
                        }
                    },
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogError(ex, "Error al obtener el contexto del bot {BotId}. Usando fallback.", botId);
                return JsonSerializer.Serialize(fallbackPayload, new JsonSerializerOptions { WriteIndented = true });
            }

            // 🔹 Extraer el system prompt y el historial del contexto
            var systemPrompt = botContext.Messages?.FirstOrDefault(m => m.Role == "system")?.Content ?? string.Empty;
            // Obtenemos los mensajes de ejemplo (user/assistant) del contexto
            var conversationHistory = botContext.Messages?.Where(m => m.Role == "user" || m.Role == "assistant").ToList() ?? new List<MessageDto>();
            // Añadir el mensaje actual del usuario al historial
            conversationHistory.Add(new MessageDto { Role = "user", Content = userMessage }); // El mensaje actual siempre va al final

            // 🔹 Mapear campos de captura, cruzando la definición con los valores ya capturados
            var captureFieldsFromContext = botContext.Capture?.Fields?
                .Select(f => new DataField
                {
                    FieldName = f.Name,
                    Value = capturedFields?.FirstOrDefault(c => c.FieldName.Equals(f.Name, StringComparison.OrdinalIgnoreCase))?.Value
                })
                .ToList() ?? new List<DataField>();

            // 🔹 Construir el JSON final con los datos dinámicos del endpoint
            var payload = new
            {
                BotId = botId,
                UserId = userId, // 👈 USADO: Usamos el UserId real
                OriginalQuestion = $"👤 Usuario dice: {userMessage}",
                UserQuestion = userMessage,
                CapturedFields = captureFieldsFromContext,
                Context = new
                {
                    SystemPrompt = systemPrompt,
                    DataCaptureStatus = BuildDataCaptureStatusPrompt(captureFieldsFromContext),
                    Resources = new
                    {
                        Documents = botContext.Training?.Documents ?? new List<string>(),
                        Urls = botContext.Training?.Urls ?? new List<string>(),
                        CustomTexts = botContext.Training?.CustomTexts ?? new List<string>(),
                        Vectors = botContext.Training?.Vectors ?? new List<object>()
                    },
                    ConversationHistory = conversationHistory.Select(m => new { m.Role, m.Content }).ToList<object>()
                },
                Timestamp = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
