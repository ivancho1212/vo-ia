using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voia.Api.Services.Interfaces;

namespace Voia.Api.Services.Chat
{
    /// <summary>
    /// 🆕 Representa un dato que fue rechazado por REGEX y será analizado por IA
    /// </summary>
    public class RejectedDataItem
    {
        public string? FieldName { get; set; }
        public string? FieldType { get; set; }
        public string? RejectedValue { get; set; }
        public string? RejectionReason { get; set; }
        public float AiConfidence { get; set; } // Confianza de IA de que fue un error
        public bool RequiresUserConfirmation { get; set; }
    }

    public class DataExtractionService
    {
        private readonly IAiProviderService _aiProviderService;
        private readonly ILogger<DataExtractionService> _logger;
        private bool _useAiExtraction = false; // 🆕 Flag para habilitar/deshabilitar IA

        public DataExtractionService(IAiProviderService aiProviderService, ILogger<DataExtractionService> logger)
        {
            _aiProviderService = aiProviderService;
            _logger = logger;
        }

        /// <summary>
        /// 🆕 MÉTODO HÍBRIDO INTELIGENTE: Analiza datos rechazados por REGEX
        /// 
        /// Flujo:
        /// 1. REGEX rechaza un dato (muy estricto)
        /// 2. Guardamos el rechazo
        /// 3. IA analiza si fue un error
        /// 4. Si IA cree que sí → Pregunta al usuario
        /// 5. Usuario confirma → Se guarda
        /// 
        /// Esto permite recuperar datos que REGEX fue demasiado estricto
        /// </summary>
        public async Task<Dictionary<string, string>> AnalyzeRejectedDataAsync(
            int botId,
            string userMessage,
            List<RejectedDataItem> rejectedItems,
            List<DataField> allFields)
        {
            var recoveredData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!rejectedItems.Any())
            {
                return recoveredData;
            }

            _logger.LogInformation("🔍 [DataExtractionService] ANÁLISIS DE DATOS RECHAZADOS - {count} items", rejectedItems.Count);

            foreach (var rejected in rejectedItems)
            {
                _logger.LogInformation("  📋 Analizando rechazado: Field='{field}', Value='{value}', Reason='{reason}'",
                    rejected.FieldName, rejected.RejectedValue, rejected.RejectionReason);

                // Usar IA para analizar si el rechazo fue un error
                var aiAnalysis = await AnalyzeRejectionWithAiAsync(botId, rejected, userMessage);

                if (aiAnalysis.wasRejectionError && aiAnalysis.confidence > 0.7f)
                {
                    _logger.LogInformation("    ⚠️ IA detectó posible error en rechazo (confidence: {conf}%)", 
                        (int)(aiAnalysis.confidence * 100));

                    rejected.RequiresUserConfirmation = true;
                    rejected.AiConfidence = aiAnalysis.confidence;
                    
                    // En un caso real, aquí se enviaría pregunta al usuario
                    // Por ahora, simulamos que el usuario confirma si confianza > 80%
                    if (aiAnalysis.confidence > 0.8f)
                    {
                        recoveredData[rejected.FieldName!] = rejected.RejectedValue!;
                        _logger.LogInformation("    ✅ Datos recuperados por IA (confidence alta): {field}={value}", 
                            rejected.FieldName, rejected.RejectedValue);
                    }
                    else
                    {
                        _logger.LogInformation("    ❓ Requiere confirmación del usuario: {field}={value}", 
                            rejected.FieldName, rejected.RejectedValue);
                        // Aquí se marcaría para preguntar al usuario públicamente
                    }
                }
                else
                {
                    _logger.LogInformation("    ✓ Rechazo validado como correcto por IA");
                }
            }

            _logger.LogInformation("🔍 [DataExtractionService] Análisis completado - {count} datos recuperados", 
                recoveredData.Count);

            return recoveredData;
        }

        /// <summary>
        /// 🆕 IA analiza si un rechazo fue un error de REGEX
        /// Retorna: (wasError, confidence)
        /// </summary>
        private async Task<(bool wasRejectionError, float confidence)> AnalyzeRejectionWithAiAsync(
            int botId,
            RejectedDataItem rejected,
            string userMessage)
        {
            try
            {
                var analysisPrompt = $@"You are a data validation expert. Analyze if this data was incorrectly rejected.

CONTEXT:
- User message: ""{userMessage}""
- Field name: {rejected.FieldName}
- Field type: {rejected.FieldType}
- Rejected value: ""{rejected.RejectedValue}""
- Rejection reason: {rejected.RejectionReason}

TASK:
Analyze if this value should have been accepted for this field.

Return a JSON object with:
{{
  ""wasError"": true/false (was the rejection wrong?),
  ""confidence"": 0.0-1.0 (how confident are you?),
  ""explanation"": ""brief explanation""
}}

Example:
{{
  ""wasError"": true,
  ""confidence"": 0.95,
  ""explanation"": ""'suba gaitana' is a valid neighborhood name (dirección), not profanity""
}}

Analyze now:";

                _logger.LogInformation("    🤖 Consultando IA sobre rechazo...");
                
                var aiResponse = await _aiProviderService.GetBotResponseAsync(botId, 0, analysisPrompt, new List<DataField>());

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogWarning("    ⚠️ IA devolvió respuesta vacía");
                    return (false, 0f);
                }

                _logger.LogInformation("    🤖 Respuesta IA: {response}", aiResponse);

                var jsonResponse = JsonDocument.Parse(aiResponse).RootElement;
                
                bool wasError = jsonResponse.GetProperty("wasError").GetBoolean();
                float confidence = (float)jsonResponse.GetProperty("confidence").GetDouble();
                string explanation = jsonResponse.GetProperty("explanation").GetString() ?? "";

                _logger.LogInformation("    📊 Análisis: wasError={error}, confidence={conf}, reason={reason}", 
                    wasError, confidence, explanation);

                return (wasError, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "    ❌ Error en análisis IA de rechazo");
                return (false, 0f);
            }
        }

        /// <summary>
        /// Extrae datos de forma HÍBRIDA: 
        /// 1. Primero intenta REGEX (para casos simples)
        /// 2. Si falla, intenta IA (para casos complejos)
        /// 3. Si no hay IA o IA falla, retorna vacío
        /// 
        /// IMPORTANTE: IA es necesaria para casos complejos como:
        /// - "me llama ivan daniel herrera surmay" (múltiples palabras)
        /// - "mi nombre es carlos y mi apellido es peña nieto" (formato múltiple)
        /// - "soy jorge cardenas" (variaciones de formato)
        /// </summary>
        public async Task<Dictionary<string, string>> ExtractDataWithAiAsync(int botId, string userMessage, List<DataField> pendingFields)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || !pendingFields.Any())
            {
                return new Dictionary<string, string>();
            }

            _logger.LogInformation("🔍 [DataExtractionService] INICIANDO EXTRACCIÓN - Mensaje: '{msg}', Campos: {fields}", 
                userMessage, string.Join(", ", pendingFields.Select(f => f.FieldName)));

            // 🆕 PASO 1: Intentar con REGEX inteligente PRIMERO (rápido, sin IA)
            // ✨ USAR VERSIÓN CON RASTREO DE RECHAZOS
            var (regexExtractedData, rejectedItems) = ExtractDataWithRegexAndRejections(userMessage, pendingFields);
            
            if (regexExtractedData.Any())
            {
                _logger.LogInformation("✅ [DataExtractionService] REGEX extrajo {count} campos exitosamente (CASOS SIMPLES)", regexExtractedData.Count);
                foreach (var item in regexExtractedData)
                {
                    _logger.LogInformation("  ✓ {field} = {value}", item.Key, item.Value);
                }
                
                // 🆕 PASO 1.5: Analizar datos rechazados si es necesario
                if (rejectedItems.Any() && regexExtractedData.Count < pendingFields.Count)
                {
                    _logger.LogInformation("🔍 [DataExtractionService] Analizando {count} datos rechazados para posible recuperación...", rejectedItems.Count);
                    
                    var recoveredData = await AnalyzeRejectedDataAsync(botId, userMessage, rejectedItems, pendingFields);
                    
                    if (recoveredData.Any())
                    {
                        _logger.LogInformation("✅ [DataExtractionService] Recuperados {count} datos de análisis IA", recoveredData.Count);
                        foreach (var item in recoveredData)
                        {
                            if (!regexExtractedData.ContainsKey(item.Key))
                            {
                                regexExtractedData[item.Key] = item.Value;
                                _logger.LogInformation("  + {field} = {value} (recuperado de rechazos)", item.Key, item.Value);
                            }
                        }
                    }
                }
                
                return regexExtractedData;
            }

            _logger.LogInformation("ℹ️ [DataExtractionService] Regex no extrajo datos (POSIBLE CASO COMPLEJO). Intentando con IA...");

            // PASO 2: Si REGEX no funcionó, intentar con IA
            return await ExtractDataWithAiInternalAsync(botId, userMessage, pendingFields);
        }

        /// <summary>
        /// 🆕 Extracción inteligente con REGEX - Similar a cómo entiendes conversaciones naturales
        /// 🔄 REFACTORIZADO: Usa método genérico parametrizado que funciona para CUALQUIER campo
        /// </summary>
        private Dictionary<string, string> ExtractDataWithRegex(string userMessage, List<DataField> pendingFields)
        {
            var extractedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var messageLower = userMessage.ToLower();

            _logger.LogInformation("🔎 [DataExtractionService] Analizando con regex patterns...");

            foreach (var field in pendingFields)
            {
                var fieldNameLower = field.FieldName.ToLower();
                string? extractedValue = null;

                // 🆕 Usar método genérico parametrizado que funciona para CUALQUIER campo
                // Recibe el nombre del campo dinámicamente de la BD
                extractedValue = ExtractListValuesGeneric(userMessage, field.FieldName);

                if (!string.IsNullOrWhiteSpace(extractedValue))
                {
                    extractedData[field.FieldName] = extractedValue.Trim();
                    _logger.LogInformation("  ✅ {field} = '{value}' [regex genérico]", field.FieldName, extractedValue.Trim());
                }
                else
                {
                    _logger.LogInformation("  ❌ {field} = NO ENCONTRADO", field.FieldName);
                }
            }

            return extractedData;
        }

        /// <summary>
        /// ✨ RASTREO REAL: Extrae datos Y captura todos los rechazos 
        /// Retorna: (extractedData, allRejections)
        /// </summary>
        private (Dictionary<string, string> extractedData, List<RejectedDataItem> rejections) ExtractDataWithRegexAndRejections(
            string userMessage, 
            List<DataField> pendingFields)
        {
            var extractedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var allRejections = new List<RejectedDataItem>();
            var messageLower = userMessage.ToLower();

            _logger.LogInformation("🔎 [DataExtractionService] Analizando con regex patterns (RASTREO REAL DE RECHAZOS)...");

            foreach (var field in pendingFields)
            {
                var fieldNameLower = field.FieldName.ToLower();
                
                // Usar la versión con rechazos
                var (extractedValue, fieldRejections) = ExtractListValuesGenericWithRejections(userMessage, field.FieldName);

                if (!string.IsNullOrWhiteSpace(extractedValue))
                {
                    extractedData[field.FieldName] = extractedValue.Trim();
                    _logger.LogInformation("  ✅ {field} = '{value}' [regex genérico]", field.FieldName, extractedValue.Trim());
                }
                else
                {
                    _logger.LogInformation("  ❌ {field} = NO ENCONTRADO", field.FieldName);
                }

                // Agregar todos los rechazos de este campo
                if (fieldRejections.Any())
                {
                    _logger.LogInformation("  📝 {field} - {count} valores rechazados", field.FieldName, fieldRejections.Count);
                    allRejections.AddRange(fieldRejections);
                }
            }

            return (extractedData, allRejections);
        }

        /// <summary>
        /// 🆕 MÉTODO GENÉRICO PARAMETRIZADO: Extrae valores para CUALQUIER campo dinámicamente
        /// 
        /// Este método reutiliza la lógica probada de ExtractName pero la parametriza
        /// para trabajar con CUALQUIER campo que venga de la BD (Nombre, Dirección, Email, etc.)
        /// 
        /// Funciona para:
        /// - Un solo valor: "mi nombre es ivan"
        /// - Listas: "ivan, pedro, carlos"
        /// - Campos mixtos: "3 nombres Y 3 direcciones Y 1 email"
        /// 
        /// Parámetro fieldName: "Nombre", "Dirección", "Email", "Teléfono", o cualquier otro
        /// </summary>
        private string? ExtractListValuesGeneric(string userMessage, string fieldName)
        {
            var extractedValues = new List<string>();
            var fieldNameLower = fieldName.ToLower();
            var messageLower = userMessage.ToLower();
            var commonWords = new[] { "hola", "estos", "son", "los", "de", "las", "que", "van", "para", "por", "el", "la", "y" };
            
            // 🆕 Auto-detectar el tipo de campo basado en su nombre
            var fieldType = AutoDetectFieldType(fieldName);
            
            _logger.LogInformation("  🔍 ExtractListValuesGeneric - Field: '{field}', Type: '{type}', Input: '{msg}'", 
                fieldName, fieldType, userMessage.Length > 80 ? userMessage.Substring(0, 80) + "..." : userMessage);

            // PASO 1: Buscar frases explícitas específicas del campo
            // Ejemplos: "mi nombre es X", "mi dirección es X", "mi email es X"
            var explicitPatterns = new[]
            {
                $@"(?:mi\s+)?{Regex.Escape(fieldNameLower)}\s+(?:es\s+|son\s+)?([a-záéíóúñ0-9@.\-\s][a-záéíóúñ0-9@.\-\s]*?)(?:\s+y\s+|,|;|\s+{Regex.Escape(fieldNameLower)}|\s+(?:nombre|dirección|teléfono|email|ciudad)|$)",
                $@"(?:soy|tengo|mi)\s+{Regex.Escape(fieldNameLower)}\s+(?:es\s+)?([a-záéíóúñ0-9@.\-\s]+?)(?:\s+y\s+|,|;|$)",
            };

            foreach (var pattern in explicitPatterns)
            {
                var matches = Regex.Matches(userMessage, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var value = match.Groups[1].Value.Trim();
                        if (IsValidFieldValue(value, fieldName, fieldType, commonWords))
                        {
                            extractedValues.Add(value);
                            _logger.LogInformation("    ✅ Valor explícito encontrado (patrón): '{value}'", value);
                        }
                    }
                }
            }

            // PASO 2: Si encontró valores explícitos, buscar también en el resto del mensaje
            if (extractedValues.Count > 0)
            {
                var afterMatch = Regex.Match(userMessage, 
                    $@"{Regex.Escape(fieldNameLower)}\s+(?:es|son|:)?\s*(.+?)(?:\s+(?:y|,|;|nombre|dirección|teléfono|email|ciudad)|$)", 
                    RegexOptions.IgnoreCase);
                if (afterMatch.Success)
                {
                    var restOfMessage = afterMatch.Groups[1].Value;
                    var listValues = ExtractListFromText(restOfMessage, fieldName, fieldType, commonWords);
                    extractedValues.AddRange(listValues);
                }
            }

            // PASO 3: Si NO encontró valores explícitos, buscar listas genéricamente
            if (extractedValues.Count == 0)
            {
                var listValues = ExtractListFromText(userMessage, fieldName, fieldType, commonWords);
                extractedValues.AddRange(listValues);
            }

            // PASO 4: Retornar valores únicos
            if (extractedValues.Any())
            {
                var uniqueValues = extractedValues
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                
                var result = string.Join(", ", uniqueValues);
                _logger.LogInformation("  ✅ Capturados {count} valores para {field}: {values}", 
                    uniqueValues.Count, fieldName, result);
                return result;
            }

            return null;
        }

        /// <summary>
        /// ✨ RASTREO REAL: Extrae valores Y captura todos los rechazos
        /// Retorna: (values, rejections)
        /// </summary>
        private (string? values, List<RejectedDataItem> rejections) ExtractListValuesGenericWithRejections(
            string userMessage, 
            string fieldName)
        {
            var extractedValues = new List<string>();
            var allRejections = new List<RejectedDataItem>();
            var fieldNameLower = fieldName.ToLower();
            var messageLower = userMessage.ToLower();
            var commonWords = new[] { "hola", "estos", "son", "los", "de", "las", "que", "van", "para", "por", "el", "la", "y" };
            
            var fieldType = AutoDetectFieldType(fieldName);
            
            _logger.LogInformation("  🔍 ExtractListValuesGenericWithRejections - Field: '{field}', Type: '{type}'", 
                fieldName, fieldType);

            // PASO 1: Patrones explícitos
            var explicitPatterns = new[]
            {
                $@"(?:mi\s+)?{Regex.Escape(fieldNameLower)}\s+(?:es\s+|son\s+)?([a-záéíóúñ0-9@.\-\s][a-záéíóúñ0-9@.\-\s]*?)(?:\s+y\s+|,|;|\s+{Regex.Escape(fieldNameLower)}|\s+(?:nombre|dirección|teléfono|email|ciudad)|$)",
                $@"(?:soy|tengo|mi)\s+{Regex.Escape(fieldNameLower)}\s+(?:es\s+)?([a-záéíóúñ0-9@.\-\s]+?)(?:\s+y\s+|,|;|$)",
            };

            foreach (var pattern in explicitPatterns)
            {
                var matches = Regex.Matches(userMessage, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var value = match.Groups[1].Value.Trim();
                        var (isValid, reason) = IsValidFieldValueWithReason(value, fieldName, fieldType, commonWords);
                        
                        if (isValid)
                        {
                            extractedValues.Add(value);
                            _logger.LogInformation("    ✅ Valor explícito encontrado: '{value}'", value);
                        }
                        else
                        {
                            allRejections.Add(new RejectedDataItem
                            {
                                FieldName = fieldName,
                                FieldType = fieldType,
                                RejectedValue = value,
                                RejectionReason = reason ?? "Razón desconocida"
                            });
                            _logger.LogInformation("    ❌ Valor explícito rechazado: '{value}' | Razón: {reason}", value, reason);
                        }
                    }
                }
            }

            // PASO 2: Si encontró valores explícitos, buscar más en resto del mensaje
            if (extractedValues.Count > 0)
            {
                var afterMatch = Regex.Match(userMessage, 
                    $@"{Regex.Escape(fieldNameLower)}\s+(?:es|son|:)?\s*(.+?)(?:\s+(?:y|,|;|nombre|dirección|teléfono|email|ciudad)|$)", 
                    RegexOptions.IgnoreCase);
                if (afterMatch.Success)
                {
                    var restOfMessage = afterMatch.Groups[1].Value;
                    var (moreValues, moreRejections) = ExtractListFromTextWithRejections(restOfMessage, fieldName, fieldType, commonWords);
                    extractedValues.AddRange(moreValues);
                    allRejections.AddRange(moreRejections);
                }
            }

            // PASO 3: Si NO encontró valores explícitos, buscar genéricamente
            if (extractedValues.Count == 0)
            {
                var (genericValues, genericRejections) = ExtractListFromTextWithRejections(userMessage, fieldName, fieldType, commonWords);
                extractedValues.AddRange(genericValues);
                allRejections.AddRange(genericRejections);
            }

            // PASO 4: Retornar valores únicos
            if (extractedValues.Any())
            {
                var uniqueValues = extractedValues
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                
                var result = string.Join(", ", uniqueValues);
                _logger.LogInformation("  ✅ Capturados {count} valores para {field}", 
                    uniqueValues.Count, fieldName);
                return (result, allRejections);
            }

            return (null, allRejections);
        }

        /// <summary>
        /// Ahora recibe el field_type para aplicar validación específica
        /// </summary>
        private List<string> ExtractListFromText(string text, string fieldName, string fieldType, string[] commonWords)
        {
            var values = new List<string>();
            var fieldNameLower = fieldName.ToLower();
            
            _logger.LogInformation("    🔍 ExtractListFromText - Field: '{field}', Type: '{type}', Input: '{text}'", 
                fieldName, fieldType, text.Length > 100 ? text.Substring(0, 100) + "..." : text);
            
            // PASO 1: Buscar patrón genérico "estos son los [fieldName]..." o "[fieldName]: ..."
            // Más flexible para detectar el inicio de la lista de valores
            var preface = Regex.Match(text, 
                $@"(?:estos\s+)?son\s+(?:los|las)?\s+{Regex.Escape(fieldNameLower)}s?\s+(?:de\s+)?(?:los\s+)?(?:que\s+)?(?:van\s+)?(?:a\s+)?(?:asistir|van|irán)?\s*[:.]*\s*(.+?)(?:\s+(?:la\s+)?dirección|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if (preface.Success && preface.Groups.Count > 1)
            {
                var valuesSection = preface.Groups[1].Value.Trim();
                _logger.LogInformation("    ✅ Detectado patrón de lista - Values section: '{section}'", 
                    valuesSection.Length > 100 ? valuesSection.Substring(0, 100) : valuesSection);
                text = valuesSection;
            }
            
            // PASO 2: Dividir por comas, "y", punto y coma
            var parts = Regex.Split(text, @"\s*(?:,|;|\s+y\s+)\s*");
            
            _logger.LogInformation("    📊 Split en {count} partes", parts.Length);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0) continue;
                
                // Si la parte es muy larga (probable contexto contaminado), ignorar
                if (trimmed.Length > 80)
                {
                    _logger.LogInformation("    ⊘ Parte demasiado larga (probable contexto): '{part}'", 
                        trimmed.Substring(0, 80) + "...");
                    continue;
                }
                
                // 🆕 Filtro: Si contiene salto de línea + palabras clave de sección, es contexto
                if (trimmed.Contains("\n") && Regex.IsMatch(trimmed, @"(?:dirección|email|teléfono|phone|address)", RegexOptions.IgnoreCase))
                {
                    _logger.LogInformation("    ⊘ Parte contiene transición de sección: '{part}'", trimmed);
                    continue;
                }
                
                if (IsValidFieldValue(trimmed, fieldName, fieldType, commonWords))
                {
                    values.Add(trimmed);
                    _logger.LogInformation("    ✅ Valor válido encontrado: '{value}'", trimmed);
                }
                else
                {
                    _logger.LogInformation("    ❌ Valor rechazado: '{value}' (no cumple validación)", trimmed);
                }
            }
            
            return values;
        }

        /// <summary>
        /// ✨ RASTREO REAL: Extrae valores Y captura rechazos con razones específicas
        /// Retorna: (values, rejectedItems)
        /// </summary>
        private (List<string> values, List<RejectedDataItem> rejections) ExtractListFromTextWithRejections(
            string text, 
            string fieldName, 
            string fieldType, 
            string[] commonWords)
        {
            var values = new List<string>();
            var rejections = new List<RejectedDataItem>();
            var fieldNameLower = fieldName.ToLower();
            
            _logger.LogInformation("    🔍 ExtractListFromTextWithRejections - Field: '{field}', Type: '{type}'", 
                fieldName, fieldType);
            
            // PASO 1: Buscar patrón genérico "estos son los [fieldName]..." o "[fieldName]: ..."
            var preface = Regex.Match(text, 
                $@"(?:estos\s+)?son\s+(?:los|las)?\s+{Regex.Escape(fieldNameLower)}s?\s+(?:de\s+)?(?:los\s+)?(?:que\s+)?(?:van\s+)?(?:a\s+)?(?:asistir|van|irán)?\s*[:.]*\s*(.+?)(?:\s+(?:la\s+)?dirección|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if (preface.Success && preface.Groups.Count > 1)
            {
                text = preface.Groups[1].Value.Trim();
            }
            
            // PASO 2: Dividir por comas, "y", punto y coma
            var parts = Regex.Split(text, @"\s*(?:,|;|\s+y\s+)\s*");
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0) continue;
                
                // Si es muy larga, es contexto
                if (trimmed.Length > 80)
                {
                    rejections.Add(new RejectedDataItem
                    {
                        FieldName = fieldName,
                        FieldType = fieldType,
                        RejectedValue = trimmed,
                        RejectionReason = "Texto demasiado largo (probable contexto contaminado)"
                    });
                    continue;
                }
                
                // Si tiene transición de sección, es contexto
                if (trimmed.Contains("\n") && Regex.IsMatch(trimmed, @"(?:dirección|email|teléfono|phone|address)", RegexOptions.IgnoreCase))
                {
                    rejections.Add(new RejectedDataItem
                    {
                        FieldName = fieldName,
                        FieldType = fieldType,
                        RejectedValue = trimmed,
                        RejectionReason = "Contiene transición a otra sección"
                    });
                    continue;
                }
                
                // Validar con captura de razón
                var (isValid, rejectionReason) = IsValidFieldValueWithReason(trimmed, fieldName, fieldType, commonWords);
                
                if (isValid)
                {
                    values.Add(trimmed);
                    _logger.LogInformation("    ✅ Valor válido: '{value}'", trimmed);
                }
                else
                {
                    rejections.Add(new RejectedDataItem
                    {
                        FieldName = fieldName,
                        FieldType = fieldType,
                        RejectedValue = trimmed,
                        RejectionReason = rejectionReason ?? "Razón desconocida"
                    });
                    _logger.LogInformation("    ❌ Valor rechazado: '{value}' | Razón: {reason}", trimmed, rejectionReason);
                }
            }
            
            return (values, rejections);
        }

        /// <summary>
        /// 🆕 VALIDACIÓN INTELIGENTE Y ESPECÍFICA POR TIPO DE CAMPO
        /// Analiza el tipo de campo y aplica reglas de validación tailorizadas
        /// 
        /// Tipos soportados (auto-detectados por el nombre):
        /// - email: Correos electrónicos
        /// - phone: Números telefónicos
        /// - address: Direcciones físicas
        /// - name: Nombres de personas
        /// - date: Fechas
        /// - number: Números
        /// - text: Texto general (default)
        /// 
        /// ✨ RETORNA TUPLA: (esValido, razonRechazo?)
        /// </summary>
        private (bool isValid, string? rejectionReason) IsValidFieldValueWithReason(string text, string fieldName, string fieldType, string[] commonWords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, "Texto vacío o nulo");

            var fieldNameLower = fieldName.ToLower();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Validaciones básicas universales
            if (text.Length < 2) 
                return (false, "Texto muy corto (menos de 2 caracteres)");
            if (words.Length > 6) 
                return (false, "Demasiadas palabras para un valor de campo");
            if (commonWords.Contains(words[0].ToLower())) 
                return (false, "Comienza con palabra común no relevante");

            _logger.LogInformation("      🔎 Validando: '{value}' | Tipo: {type}", text, fieldType);

            // 🔹 VALIDACIÓN ESPECÍFICA POR TIPO DE CAMPO
            return fieldType.ToLower() switch
            {
                // 📧 EMAILS - Validar formato estricto
                "email" or "correo" => 
                    Regex.IsMatch(text, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
                        ? (true, null)
                        : (false, "No tiene formato válido de email"),

                // 📞 TELÉFONOS - Debe tener números y caracteres telefónicos
                "phone" or "teléfono" or "telefono" or "celular" or "móvil" => 
                    (Regex.IsMatch(text, @"[\d\-\(\)\+]") && text.Length >= 7)
                        ? (true, null)
                        : (false, "No tiene formato de teléfono válido (mínimo 7 dígitos)"),

                // 📍 DIRECCIONES - Usa método especializado
                "address" or "dirección" or "direccion" => ValidateAddress(text),

                // 👤 NOMBRES - Usa método especializado
                "name" or "nombre" => ValidateName(text),

                // 📅 FECHAS - Formato de fecha
                "date" or "fecha" => 
                    Regex.IsMatch(text, @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4}|(?:enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|octubre|noviembre|diciembre))", RegexOptions.IgnoreCase)
                        ? (true, null)
                        : (false, "No tiene formato de fecha válido"),

                // 🔢 NÚMEROS - Solo dígitos y decimales
                "number" or "número" or "cantidad" => 
                    Regex.IsMatch(text, @"^\d+([.,]\d+)?$")
                        ? (true, null)
                        : (false, "No es un número válido"),

                // 📝 TEXTO GENERAL O DESCONOCIDO
                _ => ValidateGenericTextWithReason(text)
            };
        }

        /// <summary>
        /// ✨ COMPATIBILIDAD: Versión antigua que retorna bool
        /// Usa la nueva versión y descarta la razón
        /// </summary>
        private bool IsValidFieldValue(string text, string fieldName, string fieldType, string[] commonWords)
        {
            var (isValid, _) = IsValidFieldValueWithReason(text, fieldName, fieldType, commonWords);
            return isValid;
        }

        /// <summary>
        /// ✨ RASTREO REAL: Valida dirección y retorna razón de rechazo
        /// Retorna: (esValida, razonRechazo?)
        /// Si esValida = true, razonRechazo será null
        /// Si esValida = false, razonRechazo contendrá la razón específica
        /// </summary>
        private (bool isValid, string? rejectionReason) ValidateAddress(string text)
        {
            // Palabras clave que indican una dirección
            var addressKeywords = @"(calle|carrera|avenida|diagonal|pasaje|transversal|número|no\.|n°|piso|apartamento|apt|bloque|manzana|lote|vereda|vía|plaza|parque|zona|barrio|ciudad|municipio|departamento|provincia|cra|dg)";
            bool hasAddressKeyword = Regex.IsMatch(text, addressKeywords, RegexOptions.IgnoreCase);
            
            // Detectar si tiene números de dirección (formato típico: "123", "1A", "#45")
            bool hasAddressNumbers = Regex.IsMatch(text, @"(?:^|\s)(?:#|no\.?|n°)?\s*\d+[a-zA-Z]?(?:\s|$)");
            
            // Detectar si es un nombre puro (solo letras españolas, sin números ni palabras de dirección)
            bool looksLikePureName = !hasAddressKeyword && 
                                     !hasAddressNumbers &&
                                     Regex.IsMatch(text, @"^[a-záéíóúñ\s]+$", RegexOptions.IgnoreCase);
            
            // Si parece un nombre puro, NO es una dirección
            if (looksLikePureName)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' parece ser un nombre puro, no una dirección", text);
                return (false, "Parece ser un nombre puro, no contiene características de dirección");
            }
            
            // 🆕 Rechazar si es profanidad/ruido
            var profanityPatterns = @"(puta|mierda|verga|coño|culo)";
            if (Regex.IsMatch(text, profanityPatterns, RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' contiene palabras inapropiadas", text);
                return (false, "Contiene palabras inapropiadas o ruido");
            }
            
            // Es válida si tiene palabra clave de dirección O números de dirección
            bool isValid = hasAddressKeyword || hasAddressNumbers;
            if (!isValid)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' no tiene características de dirección", text);
                return (false, "No contiene palabras clave de dirección ni números de dirección");
            }
            
            return (true, null);
        }

        /// <summary>
        /// ✨ RASTREO REAL: Valida nombre y retorna razón de rechazo
        /// Retorna: (esValido, razonRechazo?)
        /// Si esValido = true, razonRechazo será null
        /// Si esValido = false, razonRechazo contendrá la razón específica
        /// </summary>
        private (bool isValid, string? rejectionReason) ValidateName(string text)
        {
            // Palabras clave que indican una dirección (para evitar confundir)
            var addressKeywords = @"(calle|carrera|avenida|diagonal|número|no\.|n°|piso|apartamento|apt|bloque|cra|dg|manzana|vereda|lote|vía|plaza|pasaje)";
            bool hasAddressKeyword = Regex.IsMatch(text, addressKeywords, RegexOptions.IgnoreCase);
            
            // Si tiene palabras de dirección, NO es un nombre
            if (hasAddressKeyword)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' contiene palabras de dirección", text);
                return (false, "Contiene palabras clave de dirección");
            }
            
            // 🆕 Filtro de profanidad/ruido común
            var profanityPatterns = @"(puta|mierda|verga|coño|culo|fuck|shit|ass)";
            if (Regex.IsMatch(text, profanityPatterns, RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' contiene palabras inapropiadas", text);
                return (false, "Contiene profanidad o palabras inapropiadas");
            }
            
            // 🆕 Rechazar si es principalmente números (no es nombre)
            var numberCount = Regex.Matches(text, @"\d").Count;
            if (numberCount > text.Length * 0.4)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' tiene demasiados números", text);
                return (false, "Contiene demasiados números para ser un nombre");
            }
            
            // Debe tener principalmente letras españolas (al menos 60%)
            var letterCount = Regex.Matches(text, @"[a-záéíóúñA-ZÁÉÍÓÚÑ]").Count;
            bool isValid = letterCount >= text.Length * 0.6;
            
            if (!isValid)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' no tiene suficientes letras para ser nombre", text);
                return (false, "Insuficientes letras para ser un nombre válido (requiere mínimo 60% alfabético)");
            }
            
            return (true, null);
        }

        /// <summary>
        /// Valida texto genérico/desconocido
        /// Lógica: Sin caracteres especiales peligrosos, con al menos 40% alfanuméricos
        /// </summary>
        private bool ValidateGenericText(string text)
        {
            // Rechazar caracteres especiales peligrosos
            if (Regex.IsMatch(text, @"[\(\)\[\]\{\}<>#$%&*]"))
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' contiene caracteres especiales", text);
                return false;
            }
            
            // Debe tener al menos 40% de caracteres alfanuméricos
            var alphanumericCount = Regex.Matches(text, @"[a-záéíóúñA-ZÁÉÍÓÚÑ0-9]").Count;
            bool isValid = alphanumericCount >= text.Length * 0.4;
            
            if (!isValid)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' no tiene suficientes caracteres alfanuméricos", text);
            }
            return isValid;
        }

        /// <summary>
        /// ✨ RASTREO REAL: Valida texto genérico y retorna razón
        /// </summary>
        private (bool isValid, string? rejectionReason) ValidateGenericTextWithReason(string text)
        {
            // Rechazar caracteres especiales peligrosos
            if (Regex.IsMatch(text, @"[\(\)\[\]\{\}<>#$%&*]"))
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' contiene caracteres especiales", text);
                return (false, "Contiene caracteres especiales peligrosos");
            }
            
            // Debe tener al menos 40% de caracteres alfanuméricos
            var alphanumericCount = Regex.Matches(text, @"[a-záéíóúñA-ZÁÉÍÓÚÑ0-9]").Count;
            bool isValid = alphanumericCount >= text.Length * 0.4;
            
            if (!isValid)
            {
                _logger.LogInformation("        ⊗ Rechazado: '{text}' no tiene suficientes caracteres alfanuméricos", text);
                return (false, "Insuficientes caracteres alfanuméricos (requiere mínimo 40%)");
            }
            return (true, null);
        }

        /// <summary>
        /// 🆕 AUTO-DETECTAR TIPO DE CAMPO basado en su nombre
        /// Si el campo no tiene tipo explícito en la BD, lo detecta automáticamente
        /// </summary>
        private string AutoDetectFieldType(string fieldName)
        {
            var fieldLower = fieldName.ToLower();

            if (fieldLower.Contains("email") || fieldLower.Contains("correo") || fieldLower.Contains("mail"))
                return "email";
            
            if (fieldLower.Contains("teléfono") || fieldLower.Contains("telefono") || fieldLower.Contains("phone") || fieldLower.Contains("móvil") || fieldLower.Contains("celular"))
                return "phone";
            
            if (fieldLower.Contains("dirección") || fieldLower.Contains("direccion") || fieldLower.Contains("address") || fieldLower.Contains("domicilio"))
                return "address";
            
            if (fieldLower.Contains("fecha") || fieldLower.Contains("date") || fieldLower.Contains("nacimiento"))
                return "date";
            
            if (fieldLower.Contains("número") || fieldLower.Contains("numero") || fieldLower.Contains("number") || fieldLower.Contains("cantidad"))
                return "number";
            
            if (fieldLower.Contains("nombre") || fieldLower.Contains("name") || fieldLower.Contains("apellido") || fieldLower.Contains("nombre completo"))
                return "name";

            return "text"; // Default
        }

        /// <summary>
        /// Extrae nombres inteligentemente - CAPTURA TODOS LOS NOMBRES disponibles
        /// Ya sea nombres personales, listas, o múltiples menciones
        /// 
        /// Estrategia:
        /// 1. Busca frases explícitas ("mi nombre es X", "me llamo X")
        /// 2. Luego busca listas de nombres (separadas por comas o "y")
        /// 3. Filtra palabras incompletas, cortas o muy comunes
        /// 
        /// Ejemplos:
        /// - "mi nombre es pipe socarras" → "Pipe Socarras"
        /// - "juan, pedro, maría" → "Juan, Pedro, María"
        /// - "estos son: carlos, ana y luis" → "Carlos, Ana, Luis"
        /// </summary>
        private string? ExtractName(string userMessage, string messageLower)
        {
            var extractedNames = new List<string>();
            var veryCommonWords = new[] { "hola", "estos", "son", "los", "nombres", "de", "las", "personas", "que", "van", "asistir", "al", "evento", "para", "por", "el", "la" };
            
            // PASO 1: Buscar frases explícitas
            var explicitPatterns = new[]
            {
                @"(?:mi\s+)?nombre\s+(?:es\s+)?([a-záéíóúñ][a-záéíóúñ\s]*?[a-záéíóúñ])(?:\s+y\s+|,|\s+vivo|\s+dirección|$)",
                @"(?:soy|me\s+llamo)\s+([a-záéíóúñ][a-záéíóúñ\s]*?[a-záéíóúñ])(?:\s+y\s+|,|\s+vivo|\s+dirección|$)",
            };

            foreach (var pattern in explicitPatterns)
            {
                var matches = Regex.Matches(userMessage, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var value = match.Groups[1].Value.Trim();
                        if (IsValidName(value, veryCommonWords))
                        {
                            extractedNames.Add(value);
                        }
                    }
                }
            }

            // PASO 2: Si encontró nombres explícitos, extrae también lo que viene después
            // (en caso de listas después de "mi nombre es")
            if (extractedNames.Count > 0)
            {
                // Buscar en el resto del mensaje después de "nombre es"
                var afterNameMatch = Regex.Match(userMessage, @"nombre\s+es\s+(.+?)(?:\s+(?:y|dirección|vivo|teléfono|email)|$)", RegexOptions.IgnoreCase);
                if (afterNameMatch.Success)
                {
                    var restOfMessage = afterNameMatch.Groups[1].Value;
                    var listNames = ExtractNameListFromText(restOfMessage, veryCommonWords);
                    extractedNames.AddRange(listNames);
                }
            }

            // PASO 3: Si NO encontró nombres explícitos, busca listas de nombres
            if (extractedNames.Count == 0)
            {
                var listNames = ExtractNameListFromText(userMessage, veryCommonWords);
                extractedNames.AddRange(listNames);
            }

            // PASO 4: Retornar nombres únicos
            if (extractedNames.Any())
            {
                var uniqueNames = extractedNames
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                
                var result = string.Join(", ", uniqueNames);
                _logger.LogInformation("  ✅ Capturados {count} nombres: {names}", uniqueNames.Count, result);
                return result;
            }

            return null;
        }

        /// <summary>
        /// Extrae lista de nombres del texto (separados por comas o "y")
        /// Mejora: Primero busca patrones de listas, luego extrae nombres individuales
        /// </summary>
        private List<string> ExtractNameListFromText(string text, string[] commonWords)
        {
            var names = new List<string>();
            
            _logger.LogInformation("    🔍 ExtractNameListFromText - Input: '{text}'", text.Length > 100 ? text.Substring(0, 100) + "..." : text);
            
            // PASO 1: Buscar patrón de "estos son los nombres de..." o similar
            // Extrae solo la parte después de "nombres de"
            var preface = Regex.Match(text, @"(?:estos\s+)?son\s+los\s+nombres?\s+(?:de\s+)?(?:los\s+)?(?:que\s+)?(?:van\s+)?(?:a\s+)?(?:asistir|van|irán)?\s*[:.]*\s*(.+?)$", RegexOptions.IgnoreCase);
            if (preface.Success && preface.Groups.Count > 1)
            {
                var namesSection = preface.Groups[1].Value.Trim();
                _logger.LogInformation("    ✅ Detectado patrón de lista - Nombres section: '{section}'", namesSection.Length > 100 ? namesSection.Substring(0, 100) : namesSection);
                text = namesSection; // Usar solo la parte relevante
            }
            
            // PASO 2: Dividir por comas y "y" 
            var parts = Regex.Split(text, @"\s*(?:,|y)\s+");
            
            _logger.LogInformation("    📊 Split en {count} partes", parts.Length);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0) continue;
                
                // Si la parte es muy larga (probablemente incluyó contexto), ignorar
                if (trimmed.Length > 80)
                {
                    _logger.LogInformation("    ⊘ Parte demasiado larga (probable contexto): '{part}'", trimmed.Length > 80 ? trimmed.Substring(0, 80) : trimmed);
                    continue;
                }
                
                if (IsValidName(trimmed, commonWords))
                {
                    names.Add(trimmed);
                    _logger.LogInformation("    ✅ Nombre válido encontrado: '{name}'", trimmed);
                }
                else
                {
                    _logger.LogInformation("    ❌ Nombre rechazado: '{name}' (no cumple validación)", trimmed);
                }
            }
            
            return names;
        }

        /// <summary>
        /// Valida si un texto es un nombre válido (no muy corto, no palabra común, etc.)
        /// Mejora: Acepta nombres más largos (hasta 6 palabras para casos como "juan carlos porras López González")
        /// </summary>
        private bool IsValidName(string text, string[] commonWords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Filtros básicos
            if (text.Length < 3) return false; // Muy corto
            if (words.Length > 6) return false; // Demasiadas palabras (probablemente no sea nombre)
            
            // Filtrar palabras muy comunes EN PRIMERA POSICIÓN
            if (commonWords.Contains(words[0].ToLower())) return false;
            
            // Filtrar si contiene números o caracteres especiales peligrosos
            if (Regex.IsMatch(text, @"[\d\(\)\[\]\{\}<>@#$%&*]")) return false;
            
            // Debe tener principalmente letras (al menos 50% letras españolas)
            var letterCount = Regex.Matches(text, @"[a-záéíóúñA-ZÁÉÍÓÚÑ]").Count;
            if (letterCount < text.Length * 0.5) return false;
            
            return true;
        }

        /// <summary>
        /// Verifica si una palabra es muy común (para evitar falsos positivos)
        /// </summary>
        private bool IsCommonWord(string word)
        {
            var commonWords = new[] { "es", "el", "la", "de", "para", "por", "que", "y", "los", "las", "en", "asistir", "evento", "estos", "son" };
            return commonWords.Contains(word.ToLower());
        }

        /// <summary>
        /// Extrae direcciones (calles, avenidas, números, ciudades)
        /// </summary>
        private string? ExtractAddress(string userMessage, string messageLower)
        {
            var patterns = new[]
            {
                // "mi dirección es X", "vivo en X", "dirección: X"
                @"(?:mi\s+)?dirección\s+(?:es\s+)?([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                @"vivo\s+(?:en\s+)?([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                @"(?:mi\s+)?dirección\s*:\s*([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                @"(?:mi\s+)?dirección\s*=\s*([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                @"(?:en\s+)?(?:calle|avenida|carrera|diagonal|transversal)\s+([^,.\n]*?)(?:\s+(?:y|,|.|$))"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(userMessage, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var value = match.Groups[1].Value.Trim();
                    if (value.Length > 3) // Validar longitud mínima
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extrae números telefónicos
        /// </summary>
        private string? ExtractPhone(string userMessage)
        {
            // Teléfono: +1 234 567 8900 o 2345678900 o +57 300 123 4567
            var phonePattern = @"(?:\+\d{1,3}\s?)?(?:\(\d{1,4}\)\s?)?\d{1,4}[\s.-]?\d{1,4}[\s.-]?\d{4,6}";
            var match = Regex.Match(userMessage, phonePattern);
            return match.Success ? Regex.Replace(match.Value, @"[^\d+]", "") : null;
        }

        /// <summary>
        /// Extrae correos electrónicos
        /// </summary>
        private string? ExtractEmail(string userMessage)
        {
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var match = Regex.Match(userMessage, emailPattern);
            return match.Success ? match.Value : null;
        }

        /// <summary>
        /// 🆕 Patrón genérico para campos no predeterminados
        /// </summary>
        private string? ExtractGenericField(string userMessage, string fieldName)
        {
            var fieldLower = fieldName.ToLower();
            
            // Intenta: "mi {field} es X", "{field}: X", "{field} = X"
            var patterns = new[]
            {
                $@"(?:mi\s+)?{Regex.Escape(fieldLower)}\s+(?:es\s+)?([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                $@"{Regex.Escape(fieldLower)}\s*:\s*([^,.\n]*?)(?:\s+(?:y|,|.|$))",
                $@"{Regex.Escape(fieldLower)}\s*=\s*([^,.\n]*?)(?:\s+(?:y|,|.|$))"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(userMessage, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var value = match.Groups[1].Value.Trim();
                    if (value.Length > 0)
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 🆕 Extracción con IA - Para casos complejos que REGEX no puede resolver
        /// </summary>
        private async Task<Dictionary<string, string>> ExtractDataWithAiInternalAsync(int botId, string userMessage, List<DataField> pendingFields)
        {
            var fieldNames = string.Join(", ", pendingFields.Select(f => $"'{f.FieldName}'"));

            var extractionPrompt = @"You are a world-class data extraction expert. Your task is to meticulously analyze the user's message and extract values for specific fields.

CONTEXT:
- The user is having a conversation in Spanish
- The user might provide data in various formats, with typos, or in conversational style
- Be flexible with how users provide their information

TASK:
Extract values for these fields: " + fieldNames + @"
User's message: """ + userMessage + @"""

RULES:
1. Return ONLY a valid JSON object with no additional text
2. Keys must exactly match the requested field names
3. If you find a value for a field, include it in the JSON
4. If you cannot find a value, DO NOT include that key
5. Do NOT invent or assume data
6. Capture complete values (e.g., names with apellidos)

Example response:
{
  ""Nombre"": ""Ivan Daniel Herrera Surmay"",
  ""Dirección"": ""Calle 5""
}

Extract now:";

            try
            {
                _logger.LogInformation("🤖 [DataExtractionService] Llamando a IA para extraer datos (CASOS COMPLEJOS)...");
                var aiResponse = await _aiProviderService.GetBotResponseAsync(botId, 0, extractionPrompt, new List<DataField>());

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogWarning("⚠️ [DataExtractionService] IA devolvió respuesta vacía");
                    return new Dictionary<string, string>();
                }

                _logger.LogInformation("🤖 [DataExtractionService] Respuesta IA: {response}", aiResponse);

                var jsonResponse = JsonDocument.Parse(aiResponse).RootElement;
                var extractedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in jsonResponse.EnumerateObject())
                {
                    var value = property.Value.GetString() ?? "";
                    extractedData[property.Name] = value;
                    _logger.LogInformation("  ✅ {field} = '{value}' [IA]", property.Name, value);
                }

                if (extractedData.Any())
                {
                    _logger.LogInformation("✅ [DataExtractionService] IA extrajo {count} campos exitosamente", extractedData.Count);
                }
                else
                {
                    _logger.LogWarning("⚠️ [DataExtractionService] IA procesó pero no extrajo campos");
                }

                return extractedData;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "❌ [DataExtractionService] Error: IA no devolvió JSON válido");
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DataExtractionService] Error en extracción con IA: {message}", ex.Message);
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 🆕 PHASE 3: Revisión retrospectiva de la conversación
        /// Analiza todo el historial de mensajes de la conversación para capturar datos que se hayan pasado por alto
        /// - Lee TODOS los mensajes anteriores
        /// - Busca datos extractables en cada uno
        /// - Compara con datos ya capturados para evitar duplicados
        /// - Retorna SOLO datos nuevos que no se hayan capturado aún
        /// </summary>
        public async Task<Dictionary<string, string>> ReviewConversationForMissedDataAsync(
            string conversationHistory,
            List<DataField> pendingFields,
            Dictionary<string, string> alreadyCapturedData)
        {
            var missedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(conversationHistory) || !pendingFields.Any())
            {
                return missedData;
            }

            _logger.LogInformation("🔍 [DataExtractionService - PHASE 3] Iniciando revisión retrospectiva de conversación...");
            _logger.LogInformation("   Campos a buscar: {fields}", string.Join(", ", pendingFields.Select(f => f.FieldName)));
            _logger.LogInformation("   Datos ya capturados: {data}", 
                string.Join(", ", alreadyCapturedData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

            try
            {
                // 🆕 Usar el método genérico parametrizado que funciona con ANY field type
                foreach (var field in pendingFields)
                {
                    var fieldNameLower = field.FieldName.ToLower();
                    
                    // Si ya tenemos dato para este campo, saltar
                    if (alreadyCapturedData.ContainsKey(field.FieldName))
                    {
                        _logger.LogInformation("   ⊘ Campo '{field}' ya capturado: {value}", field.FieldName, alreadyCapturedData[field.FieldName]);
                        continue;
                    }

                    // 🆕 Usar método genérico parametrizado para Phase 3 también
                    var fieldType = AutoDetectFieldType(field.FieldName);
                    string? newValue = ExtractListValuesGeneric(conversationHistory, field.FieldName);

                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        // Verificar si el valor ya está capturado (evitar duplicados)
                        if (!alreadyCapturedData.ContainsValue(newValue) && 
                            !missedData.ContainsValue(newValue))
                        {
                            missedData[field.FieldName] = newValue;
                            _logger.LogInformation("   ✅ ENCONTRADO (retrospectivo): {field} = {value}", field.FieldName, newValue);
                        }
                        else
                        {
                            _logger.LogInformation("   ⊗ DESCARTADO (duplicado): {field} = {value}", field.FieldName, newValue);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("   ✗ No encontrado: {field}", field.FieldName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DataExtractionService - PHASE 3] Error en revisión retrospectiva: {message}", ex.Message);
            }

            _logger.LogInformation("🔍 [DataExtractionService - PHASE 3] Revisión completada. Datos nuevos encontrados: {count}", missedData.Count);
            foreach (var item in missedData)
            {
                _logger.LogInformation("   + {field} = {value}", item.Key, item.Value);
            }

            return missedData;
        }
    }
}