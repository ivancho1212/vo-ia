using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Voia.Api.Models.Bots;
using Microsoft.Extensions.Logging;
using Voia.Api.Services.Security;

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
        private readonly IPromptInjectionProtectionService _promptProtection;

        // 🔹 Cambiado el valor por defecto de isMock a false para que la implementación real sea la predeterminada.
        public PromptBuilderService(
            HttpClient httpClient,
            ILogger<PromptBuilderService> logger,
            IPromptInjectionProtectionService promptProtection,
            bool isMock = false)
        {
            _httpClient = httpClient;
            _isMock = isMock;
            _logger = logger;
            _promptProtection = promptProtection;
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
        /// Detecta el idioma probable del mensaje del usuario analizando palabras clave
        /// </summary>
        private string DetectUserLanguage(string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
                return "unknown";

            // Palabras clave comunes en diferentes idiomas
            var spanishKeywords = new[] { "hola", "qué", "cómo", "para", "porque", "donde", "cuando", "gracias", "por favor", "ayuda", "sí", "no", "está", "tengo" };
            var englishKeywords = new[] { "hello", "what", "how", "please", "thank", "help", "yes", "no", "where", "when", "thanks", "can", "is", "have" };
            var frenchKeywords = new[] { "bonjour", "comment", "où", "quand", "pourquoi", "merci", "s'il vous plaît", "oui", "non", "comment ça", "ça va" };
            var portugueseKeywords = new[] { "olá", "oi", "como", "onde", "quando", "obrigado", "por favor", "sim", "não", "está", "tenho" };
            var germanKeywords = new[] { "hallo", "wie", "wo", "wann", "warum", "danke", "bitte", "ja", "nein", "ist", "habe" };

            var lowerMessage = userMessage.ToLower();

            var spanishCount = spanishKeywords.Count(kw => lowerMessage.Contains(kw));
            var englishCount = englishKeywords.Count(kw => lowerMessage.Contains(kw));
            var frenchCount = frenchKeywords.Count(kw => lowerMessage.Contains(kw));
            var portugueseCount = portugueseKeywords.Count(kw => lowerMessage.Contains(kw));
            var germanCount = germanKeywords.Count(kw => lowerMessage.Contains(kw));

            // Retornar el idioma con más coincidencias
            var detectedLanguage = "unknown";
            int maxCount = Math.Max(spanishCount, Math.Max(englishCount, Math.Max(frenchCount, Math.Max(portugueseCount, germanCount))));

            if (maxCount == 0)
                return "unknown"; // No hay coincidencias claras

            if (maxCount == spanishCount) detectedLanguage = "Spanish";
            else if (maxCount == englishCount) detectedLanguage = "English";
            else if (maxCount == frenchCount) detectedLanguage = "French";
            else if (maxCount == portugueseCount) detectedLanguage = "Portuguese";
            else if (maxCount == germanCount) detectedLanguage = "German";

            _logger.LogInformation("🔍 [LanguageDetection] Idioma detectado: {language} (Score: {score})", detectedLanguage, maxCount);
            return detectedLanguage;
        }

        /// <summary>
        /// Construye un contexto regional DINÁMICO basado en idioma y ubicación del usuario
        /// Adapta el mensaje para que la IA entienda cómo debe responder
        /// </summary>
        private string BuildDynamicRegionalContext(string city, string country, string detectedLanguage)
        {
            // Mapeo de idiomas detectados a su nombre en inglés (para que OpenAI entienda)
            var languageInEnglish = detectedLanguage switch
            {
                "Spanish" => "Spanish",
                "English" => "English",
                "French" => "French",
                "Portuguese" => "Portuguese",
                "German" => "German",
                _ => "the user's language"
            };

            // Construcción dinámica del mensaje
            var dynamicContext = $@"

---CONTEXTO DE USUARIO---
📍 UBICACIÓN: El usuario está escribiendo desde {city}, {country}
🗣️ IDIOMA: El usuario está comunicándose en {languageInEnglish}

INSTRUCCIÓN CRÍTICA:
1. Responde en {languageInEnglish} (el idioma que el usuario está usando)
2. Adapta el contenido considerando la región {city}, {country}:
   - Usa acentos, expresiones y referencias culturales de esa región
   - Menciona monedas, medidas y costumbres locales si es relevante
   - Ten en cuenta la zona horaria y contexto temporal de {country}
3. Si bien el usuario está en {city}, {country}, mantén tu respuesta auténtica a esa región
4. No cambies el idioma - responde en {languageInEnglish} tal como el usuario te escribe

EJEMPLO DE COMPORTAMIENTO:
- Usuario en Buenos Aires (Argentina) escribiendo en ESPAÑOL: Responde con acento argentino, referencias locales
- Usuario en Tokyo (Japón) escribiendo en INGLÉS: Responde en inglés, pero con contexto de Japón
- Usuario en Montreal (Canadá) escribiendo en FRANCÉS: Responde en francés, con contexto de Québec/Canadá
---FIN CONTEXTO DE USUARIO---";

            return dynamicContext;
        }

        /// <summary>
        /// Construye un payload JSON limpio para la IA, incluyendo contexto, recursos, historial y último mensaje.
        /// Funciona tanto para mock como para producción.
        /// </summary>
        public async Task<string> BuildPromptFromBotContextAsync(
            int botId,
            int userId,
            string userMessage,
            List<DataField> capturedFields,
            string? userCountry = null,
            string? userCity = null,
            string? contextMessage = null
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
                // Detectar idioma del usuario y construir contexto dinámico
                var fallbackDetectedLanguage = DetectUserLanguage(userMessage);
                var fallbackSystemPrompt = "⚠️ No se pudo obtener contexto del bot: " + ex.Message;
                
                if (!string.IsNullOrEmpty(userCountry) && !string.IsNullOrEmpty(userCity))
                {
                    var dynamicRegionalContext = BuildDynamicRegionalContext(userCity, userCountry, fallbackDetectedLanguage);
                    fallbackSystemPrompt += dynamicRegionalContext;
                    _logger.LogInformation("🌍 [PromptBuilderService-Fallback] CONTEXTO DINÁMICO AÑADIDO - Idioma: {language}, Ubicación: {city}, {country}", 
                        fallbackDetectedLanguage, userCity, userCountry);
                }
                
                var fallbackPayload = new
                {
                    Error = $"No se pudo obtener contexto del bot: {ex.Message}",
                    BotId = botId, 
                    UserId = userId,
                    UserQuestion = userMessage,
                    CapturedFields = capturedFields ?? new List<DataField>(),
                    UserLocation = new
                    {
                        Country = userCountry,
                        City = userCity
                    },
                    ContextMessage = contextMessage,
                    Context = new
                    {
                        SystemPrompt = fallbackSystemPrompt,
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
            
            // 🌍 ENRIQUECER SYSTEM PROMPT CON CONTEXTO GEOGRÁFICO Y DE IDIOMA
            // Detectar idioma del usuario desde el primer mensaje
            var detectedLanguage = DetectUserLanguage(userMessage);
            
            if (!string.IsNullOrEmpty(userCountry) && !string.IsNullOrEmpty(userCity))
            {
                // Construir contexto dinámico basado en idioma Y ubicación
                var regionContext = BuildDynamicRegionalContext(userCity, userCountry, detectedLanguage);
                systemPrompt += regionContext;
                _logger.LogInformation("🌍 [PromptBuilderService] CONTEXTO DINÁMICO AÑADIDO - Idioma: {language}, Ubicación: {city}, {country}", 
                    detectedLanguage, userCity, userCountry);
            }
            
            // 🔐 PROTECCIÓN CONTRA PROMPT INJECTION
            // Validar que el mensaje del usuario no contiene intents maliciosos
            if (_promptProtection.DetectPromptInjectionAttempt(userMessage))
            {
                _logger.LogWarning(
                    "🚨 PROMPT INJECTION ATTEMPT DETECTED: BotId={BotId}, UserId={UserId}",
                    botId, userId);
                // Sanitizar el mensaje para prevenir inyección
                userMessage = _promptProtection.SanitizeUserInput(userMessage);
            }

            // Validar seguridad del prompt completo
            var promptValidation = _promptProtection.ValidatePromptSafety(systemPrompt, userMessage);
            if (!promptValidation.IsValid)
            {
                _logger.LogWarning(
                    "⚠️ PROMPT SAFETY VALIDATION FAILED: RiskScore={RiskScore}, Issues={Issues}",
                    promptValidation.RiskScore,
                    string.Join(", ", promptValidation.Issues));
                // Aplicar sanitización adicional
                userMessage = _promptProtection.SanitizeUserInput(userMessage);
            }
            
            // Obtenemos los mensajes de ejemplo (user/assistant) del contexto
            var conversationHistory = botContext.Messages?.Where(m => m.Role == "user" || m.Role == "assistant").ToList() ?? new List<MessageDto>();
            // Añadir el mensaje actual del usuario al historial (sanitizado)
            conversationHistory.Add(new MessageDto { Role = "user", Content = userMessage });

            // 🔹 Mapear campos de captura, cruzando la definición con los valores ya capturados
            var captureFieldsFromContext = botContext.Capture?.Fields?
                .Select(f => new DataField
                {
                    FieldName = f.Name,
                    Value = capturedFields?.FirstOrDefault(c => c.FieldName.Equals(f.Name, StringComparison.OrdinalIgnoreCase))?.Value
                })
                .ToList() ?? new List<DataField>();

            // 🔹 Construir el JSON OPTIMIZADO para OpenAI - Solo lo que la IA necesita
            var payload = new
            {
                UserQuestion = userMessage,
                UserLocation = new
                {
                    Country = userCountry,
                    City = userCity
                },
                ContextMessage = contextMessage,
                SystemPrompt = systemPrompt,
                DataCaptureStatus = BuildDataCaptureStatusPrompt(captureFieldsFromContext),
                CapturedFields = captureFieldsFromContext,
                Resources = new
                {
                    Documents = botContext.Training?.Documents ?? new List<string>(),
                    Urls = botContext.Training?.Urls ?? new List<string>(),
                    CustomTexts = botContext.Training?.CustomTexts ?? new List<string>(),
                    Vectors = botContext.Training?.Vectors ?? new List<object>()
                },
                ConversationHistory = conversationHistory.Select(m => new { m.Role, m.Content }).ToList<object>()
            };

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("📍 [PromptBuilderService] JSON payload built for bot {BotId} with location: {city}, {country}", botId, userCity, userCountry);
            _logger.LogInformation("📤 [PromptBuilderService] FULL JSON PAYLOAD:\n{jsonPayload}", jsonPayload);
            
            return jsonPayload;
        }
    }
}
