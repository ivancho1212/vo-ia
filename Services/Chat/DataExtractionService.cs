using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voia.Api.Services.Interfaces;

namespace Voia.Api.Services.Chat
{
    public class DataExtractionService
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ILogger<DataExtractionService> _logger;

        public DataExtractionService(IAiProviderService aiProviderService, ILogger<DataExtractionService> logger)
        {
            _aiProviderService = aiProviderService;
            _logger = logger;
        }

        /// <summary>
        /// Usa un modelo de IA para extraer datos estructurados de un mensaje de usuario.
        /// </summary>
        /// <param name="botId">ID del bot para usar su configuración de IA.</param>
        /// <param name="userMessage">El mensaje del usuario a analizar.</param>
        /// <param name="pendingFields">La lista de campos que se están buscando.</param>
        /// <returns>Un diccionario con los nombres de los campos y los valores extraídos.</returns>
        public async Task<Dictionary<string, string>> ExtractDataWithAiAsync(int botId, string userMessage, List<DataField> pendingFields)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || !pendingFields.Any())
            {
                return new Dictionary<string, string>();
            }

            // 1. Construir el prompt para la IA
            var fieldNames = string.Join(", ", pendingFields.Select(f => $"'{f.FieldName}'"));
            var jsonStructureExample = string.Join(",\\n  ", pendingFields.Select(f => $"\\\"{f.FieldName}\\\": \\\"<valor extraído>\\\""));

            var extractionPrompt = $@"
                SYSTEM: You are a world-class data extraction expert. Your task is to meticulously analyze the user's message and extract values for specific fields.
                - The user's message might contain one or more data points, sometimes with typos or in a conversational format.
                - Be very careful to separate the values for each field. For example, if the user says 'My name is John and I live in New York', the value for 'Nombre' is 'John', not 'John and I live in New York'.
                - Requested fields to extract: {fieldNames}.
                - Analyze this user message: ""{userMessage}""
                
                Your response MUST be ONLY a valid JSON object.
                - The JSON keys must exactly match the requested field names.
                - If you find a value for a field, include it in the JSON.
                - If you cannot find a value for a field, or if the value is ambiguous, DO NOT include that key in the JSON.
                - Do not add explanations, apologies, or any text outside the JSON object.
                - Do not invent data.

                Example for a message like 'Hola, mi nombre es Juan Pérez y vivo en Calle Falsa 123.':
                {{
                  ""Nombre"": ""Juan Pérez"",
                  ""Direccion"": ""Calle Falsa 123""
                }}
                
                USER_MESSAGE:
                {userMessage}
            ";

            try
            {
                // 2. Llamar a la IA
                // Usamos userId=0 y una lista vacía de capturedFields porque esta es una llamada interna de sistema.
                var aiResponse = await _aiProviderService.GetBotResponseAsync(botId, 0, extractionPrompt, new List<DataField>());

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogWarning("La IA no devolvió respuesta para la extracción de datos.");
                    return new Dictionary<string, string>();
                }

                // 3. Parsear la respuesta JSON de la IA
                var jsonResponse = JsonDocument.Parse(aiResponse).RootElement;
                var extractedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in jsonResponse.EnumerateObject())
                {
                    extractedData[property.Name] = property.Value.GetString();
                }

                return extractedData;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de formato JSON al parsear la respuesta de la IA. La IA no devolvió un JSON válido. Prompt: {Prompt}", extractionPrompt);
                return new Dictionary<string, string>(); // Devolver vacío para que el flujo pueda continuar (ej. a Regex)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos con la IA. Prompt: {Prompt}", extractionPrompt);
                return new Dictionary<string, string>();
            }
        }
    }
}