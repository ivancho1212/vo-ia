using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voia.Api.Services.Interfaces;
using Voia.Api.Models.AiModelConfigs;

namespace Voia.Api.Services.Mocks
{
    public class MockAiProviderService : IAiProviderService
    {
        public Task<string> GetBotResponseAsync(int botId, int userId, string question, List<DataField> capturedFields = null)
        {
            // Buscar si tenemos un nombre capturado
            string nombre = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "nombre")?
                .Value;

            // Respuesta básica
            string respuesta = "[MOCK] BotId:" + botId + ", UserId:" + userId + ", pregunta: '" + question + "'";

            // Si tenemos nombre, añadimos saludo personalizado
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                respuesta += $"\n¡Hola {nombre}! Gracias por tu mensaje.";
            }

            // También se podría personalizar con dirección o teléfono si quieres
            string direccion = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "direccion")?
                .Value;
            if (!string.IsNullOrWhiteSpace(direccion))
            {
                respuesta += $"\nVeo que tu dirección es: {direccion}.";
            }

            string telefono = capturedFields?
                .FirstOrDefault(f => f.FieldName.ToLower() == "telefono")?
                .Value;
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                respuesta += $"\nTu teléfono registrado es: {telefono}.";
            }

            return Task.FromResult(respuesta);
        }
    }
}
