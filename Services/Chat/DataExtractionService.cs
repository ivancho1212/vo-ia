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
            var extractionPrompt = $@"
                SYSTEM: You are a data extraction expert. Your only task is to analyze the user's message and extract the requested fields.
                - Fields to extract: {fieldNames}.
                - Analyze the following user message: ""{userMessage}""
                - Respond ONLY with a JSON object containing the fields you found.
                - If a field is not found, do not include it in the JSON.
                - Do not add any explanation or introductory text.

                Example response for a message like 'Hi, my name is John Doe and my number is 555-1234':
                {{
                  ""Nombre"": ""John Doe"",
                  ""Telefono"": ""555-1234""
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos con la IA. Prompt: {Prompt}", extractionPrompt);
                return new Dictionary<string, string>();
            }
        }
    }
}