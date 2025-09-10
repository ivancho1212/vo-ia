using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voia.Api.Services.Interfaces;
using System.Text.Json;
using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Services.Mocks
{
    public class MockAiProviderService : IAiProviderService
    {
        public Task<string> GetBotResponseAsync(int botId, int userId, string question, List<DataField> capturedFields = null)
        {
            // 🧠 El mock ahora es más inteligente: extrae la pregunta real del prompt completo.
            string userQuestion = question;
            string userSaysMarker = "👤 Usuario dice:";
            int markerIndex = question.LastIndexOf(userSaysMarker);
            if (markerIndex != -1)
            {
                userQuestion = question.Substring(markerIndex + userSaysMarker.Length).Trim();
            }

            // Buscar si tenemos un nombre capturado
            string nombre = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "nombre")?
                .Value;

            var responseTextBuilder = new System.Text.StringBuilder();
            responseTextBuilder.Append($"[MOCK] ¡Hola! Soy el bot {botId} y recibí tu pregunta: '{userQuestion}'.");

            // Si tenemos nombre, añadimos saludo personalizado
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                responseTextBuilder.Append($" ¡Qué tal, {nombre}!");
            }

            string direccion = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "direccion")?
                .Value;
            if (!string.IsNullOrWhiteSpace(direccion))
            {
                responseTextBuilder.Append($" Veo que tu dirección es: {direccion}.");
            }

            string telefono = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "telefono")?
                .Value;
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                responseTextBuilder.Append($" Tu teléfono registrado es: {telefono}.");
            }

            // Construir el objeto de respuesta que simula la respuesta real de la IA
            var mockResponseObject = new
            {
                Answer = responseTextBuilder.ToString(),
                CapturedFields = capturedFields
            };

            // Serializar el objeto a un string JSON
            return Task.FromResult(JsonSerializer.Serialize(mockResponseObject));
        }
    }
}
