using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Voia.Api.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services.Mocks
{
    public class MockAiProviderService : IAiProviderService
    {
        public async Task<string> GetBotResponseAsync(
            int botId,
            int userId,
            string question,
            List<DataField> capturedFields = null)
        {
            // ✅ FIX: Simular la extracción de datos si el prompt lo solicita.
            if (question.Contains("You are a data extraction expert"))
            {
                var extractedData = new Dictionary<string, string>();
                
                // Usamos una regex simple para simular la extracción en el mock
                var nameMatch = System.Text.RegularExpressions.Regex.Match(question, @"(?:mi\s+nombre\s+es|me\s+llamo|soy)\s+([a-zA-Z\s]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (nameMatch.Success)
                {
                    extractedData["Nombre"] = nameMatch.Groups[1].Value.Trim();
                }

                // Podrías añadir más regex para otros campos si lo necesitas para las pruebas

                return await Task.FromResult(JsonSerializer.Serialize(extractedData));
            }

            // ✅ FIX: Para una conversación normal, el mock ahora devuelve el JSON completo que recibe.
            // El 'question' que llega aquí es el JSON construido por PromptBuilderService.
            return await Task.FromResult(question);
        }
    }
}
