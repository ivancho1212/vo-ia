using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Voia.Api.Services
{
    /// <summary>
    /// ✅ FIX C: Servicio de validación de contexto para detectar alucinaciones.
    /// 
    /// Detecta 5 patrones principales de alucinaciones:
    /// 1. Respuestas que contradicen el contexto disponible
    /// 2. Información específica sin fuente en vectores
    /// 3. Patrones de confianza excesiva (fechas precisas, números)
    /// 4. Respuestas genéricas fuera de contexto
    /// 5. Referencias a información inexistente
    /// 
    /// Retorna ContextValidationResult con:
    /// - UsedContext: bool
    /// - Confidence: 0-1
    /// - HallucinationRisk: bajo|medio|alto
    /// - Diagnosis: descripción detallada
    /// </summary>
    public class ContextValidationService
    {
        private readonly VectorRelevanceFilterService _vectorFilter;

        public ContextValidationService()
        {
            _vectorFilter = new VectorRelevanceFilterService();
        }

        /// <summary>
        /// Valida una respuesta de IA contra los vectores de contexto.
        /// </summary>
        public async Task<ContextValidationResult> ValidateResponseAsync(
            string response,
            List<object> vectors,
            string originalQuery)
        {
            var result = new ContextValidationResult();

            if (string.IsNullOrEmpty(response))
            {
                result.UsedContext = false;
                result.HallucinationRisk = "bajo";
                result.Confidence = 0;
                result.Diagnosis = "Respuesta vacía";
                return result;
            }

            // Obtener estadísticas de vectores disponibles
            var stats = _vectorFilter.GetVectorStats(vectors);
            result.AvailableVectorCount = stats.total;
            result.ValidVectorCount = stats.valid;

            // ✅ ANÁLISIS 1: ¿Se usó contexto?
            result.UsedContext = DetectContextUsage(response, vectors);

            // ✅ ANÁLISIS 2: Patrones de alucinación
            var hallucinations = DetectHallucinationPatterns(response, vectors, originalQuery);
            result.DetectedPatterns = hallucinations;

            // ✅ ANÁLISIS 3: Calcular riesgo
            var (risk, confidence) = CalculateHallucinationRisk(hallucinations, stats);
            result.HallucinationRisk = risk;
            result.Confidence = confidence;

            // ✅ ANÁLISIS 4: Generar diagnosis
            result.Diagnosis = GenerateDiagnosis(result);

            return result;
        }

        /// <summary>
        /// Detecta si la respuesta utilizó el contexto disponible.
        /// </summary>
        private bool DetectContextUsage(string response, List<object> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return false;

            // Buscar palabras clave o frases del contexto en la respuesta
            try
            {
                var responseWords = ExtractKeywords(response);

                foreach (var vector in vectors)
                {
                    if (vector is System.Collections.IDictionary dict)
                    {
                        var content = dict["original_text"]?.ToString() ?? "";
                        var vectorWords = ExtractKeywords(content);

                        // Si hay coincidencia significativa, se usó contexto
                        var overlap = responseWords.Intersect(vectorWords, StringComparer.OrdinalIgnoreCase).Count();
                        if (overlap >= 3)  // Al menos 3 palabras en común
                        {
                            return true;
                        }
                    }
                }
            }
            catch { /* ignore */ }

            return false;
        }

        /// <summary>
        /// Detecta 5 patrones de alucinación.
        /// </summary>
        private List<HallucinationPattern> DetectHallucinationPatterns(
            string response,
            List<object> vectors,
            string originalQuery)
        {
            var patterns = new List<HallucinationPattern>();

            // ✅ PATRÓN 1: Contradicción con contexto
            var contradiction = DetectContextContradiction(response, vectors);
            if (contradiction != null)
                patterns.Add(contradiction);

            // ✅ PATRÓN 2: Información específica sin fuente
            var unsouredInfo = DetectUnsourcedSpecificInfo(response, vectors);
            if (unsouredInfo != null)
                patterns.Add(unsouredInfo);

            // ✅ PATRÓN 3: Exceso de confianza
            var overconfidence = DetectOverconfidence(response);
            if (overconfidence != null)
                patterns.Add(overconfidence);

            // ✅ PATRÓN 4: Respuesta genérica fuera de contexto
            var offContext = DetectOffContextGenericResponse(response, originalQuery, vectors);
            if (offContext != null)
                patterns.Add(offContext);

            // ✅ PATRÓN 5: Referencias a información inexistente
            var falsReferences = DetectFalseReferences(response, vectors);
            if (falsReferences != null)
                patterns.Add(falsReferences);

            return patterns;
        }

        /// <summary>
        /// PATRÓN 1: Detecta si la respuesta contradice el contexto disponible.
        /// </summary>
        private HallucinationPattern? DetectContextContradiction(string response, List<object> vectors)
        {
            // Palabras negativas que típicamente indican contradicción
            var negativeIndicators = new[] { "no existe", "no se encontr", "no disponible", "no hay", "no tenemos" };

            bool hasNegation = negativeIndicators.Any(neg =>
                response.IndexOf(neg, StringComparison.OrdinalIgnoreCase) >= 0
            );

            // Si hay negación pero existen vectores con contexto relevante
            if (hasNegation && vectors != null && vectors.Count > 3)
            {
                return new HallucinationPattern
                {
                    Name = "Contradicción con Contexto",
                    Severity = "medio",
                    Description = "La respuesta niega existencia pero hay contexto disponible",
                    Evidence = $"Negación detectada en: {response.Substring(0, Math.Min(100, response.Length))}..."
                };
            }

            return null;
        }

        /// <summary>
        /// PATRÓN 2: Detecta información específica (números, fechas) sin fuente en vectores.
        /// </summary>
        private HallucinationPattern? DetectUnsourcedSpecificInfo(string response, List<object> vectors)
        {
            // Patrones para información específica
            var datePattern = new Regex(@"\d{1,2}[/-]\d{1,2}[/-]\d{2,4}");
            var numberPattern = new Regex(@"\$?\d+\.?\d*");
            var percentPattern = new Regex(@"\d+%");

            var dates = datePattern.Matches(response).Count;
            var numbers = numberPattern.Matches(response).Count;
            var percents = percentPattern.Matches(response).Count;

            int specificInfo = dates + numbers + percents;

            // Si hay mucha información específica pero poco contexto, es sospechoso
            if (specificInfo > 3 && (vectors == null || vectors.Count < 2))
            {
                return new HallucinationPattern
                {
                    Name = "Información Específica Sin Fuente",
                    Severity = "alto",
                    Description = $"Se citan {specificInfo} datos específicos sin contexto de referencia",
                    Evidence = $"Fechas: {dates}, Números: {numbers}, Porcentajes: {percents}"
                };
            }

            return null;
        }

        /// <summary>
        /// PATRÓN 3: Detecta exceso de confianza (respuestas muy definitivas).
        /// </summary>
        private HallucinationPattern? DetectOverconfidence(string response)
        {
            // Frases de confianza excesiva
            var overconfidentPhrases = new[]
            {
                "definitivamente", "sin duda", "seguro", "ciertamente", "100%",
                "la verdad es", "es un hecho que", "no hay duda de que",
                "es obvio que", "claramente", "por supuesto que"
            };

            int overconfidentCount = overconfidentPhrases.Count(phrase =>
                response.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0
            );

            if (overconfidentCount >= 2)
            {
                return new HallucinationPattern
                {
                    Name = "Exceso de Confianza",
                    Severity = "bajo",
                    Description = $"La respuesta expresa {overconfidentCount} afirmaciones muy definitivas",
                    Evidence = $"Frases: {string.Join(", ", overconfidentPhrases.Where(p => response.Contains(p, StringComparison.OrdinalIgnoreCase)))}"
                };
            }

            return null;
        }

        /// <summary>
        /// PATRÓN 4: Detecta respuestas genéricas fuera de contexto.
        /// </summary>
        private HallucinationPattern? DetectOffContextGenericResponse(
            string response,
            string originalQuery,
            List<object> vectors)
        {
            // Respuestas genéricas comunes
            var genericResponses = new[]
            {
                "Lo siento", "No sé", "No puedo", "No tengo información",
                "Según mi conocimiento", "En general", "Típicamente",
                "Por lo general", "Usualmente"
            };

            bool isGeneric = genericResponses.Any(g =>
                response.StartsWith(g, StringComparison.OrdinalIgnoreCase)
            );

            // Comparar palabras clave entre query y respuesta
            var queryWords = ExtractKeywords(originalQuery);
            var responseWords = ExtractKeywords(response);
            var overlap = queryWords.Intersect(responseWords).Count();

            if (isGeneric && overlap < 2 && vectors != null && vectors.Count > 0)
            {
                return new HallucinationPattern
                {
                    Name = "Respuesta Genérica Fuera de Contexto",
                    Severity = "medio",
                    Description = "Respuesta genérica sin relacionarse con query específica y contexto disponible",
                    Evidence = $"Query similarity: {overlap} palabras en común de {queryWords.Count}"
                };
            }

            return null;
        }

        /// <summary>
        /// PATRÓN 5: Detecta referencias a información inexistente.
        /// </summary>
        private HallucinationPattern? DetectFalseReferences(string response, List<object> vectors)
        {
            // Palabras que típicamente preceden referencias
            var referencePatterns = new[] { "según", "mencioné", "como dije", "en el documento", "en el archivo" };

            bool hasReferences = referencePatterns.Any(ref_word =>
                response.IndexOf(ref_word, StringComparison.OrdinalIgnoreCase) >= 0
            );

            // Si dice "según algo" pero no tiene vectores, es peligroso
            if (hasReferences && (vectors == null || vectors.Count == 0))
            {
                return new HallucinationPattern
                {
                    Name = "Referencias a Información Inexistente",
                    Severity = "alto",
                    Description = "La respuesta refiere a documentos/información sin tener contexto",
                    Evidence = "Lenguaje citativo detectado sin fuentes disponibles"
                };
            }

            return null;
        }

        /// <summary>
        /// Calcula el riesgo de alucinación y nivel de confianza.
        /// </summary>
        private (string risk, double confidence) CalculateHallucinationRisk(
            List<HallucinationPattern> patterns,
            (int total, int valid, double avgScore, double maxScore, double minScore) vectorStats)
        {
            if (patterns.Count == 0)
            {
                // Sin patrones de alucinación
                double confidenceValue = vectorStats.total > 0 ? Math.Min(1.0, 0.5 + (vectorStats.avgScore * 0.5)) : 0.3;
                return ("bajo", confidenceValue);
            }

            // Calcular severidad
            int altaCount = patterns.Count(p => p.Severity == "alto");
            int mediaCount = patterns.Count(p => p.Severity == "medio");
            int bajaCount = patterns.Count(p => p.Severity == "bajo");

            string risk;
            double confidence;

            if (altaCount >= 2 || (altaCount > 0 && vectorStats.total == 0))
            {
                risk = "alto";
                confidence = 0.2;
            }
            else if (altaCount > 0 || mediaCount >= 2)
            {
                risk = "medio";
                confidence = 0.4 + (vectorStats.avgScore * 0.2);
            }
            else
            {
                risk = "bajo";
                confidence = 0.6 + (vectorStats.avgScore * 0.3);
            }

            return (risk, Math.Min(1.0, confidence));
        }

        /// <summary>
        /// Genera un diagnóstico descriptivo del estado.
        /// </summary>
        private string GenerateDiagnosis(ContextValidationResult result)
        {
            var parts = new List<string>();

            parts.Add($"Vectores disponibles: {result.AvailableVectorCount}");
            parts.Add($"Contexto utilizado: {(result.UsedContext ? "Sí" : "No")}");
            parts.Add($"Patrones detectados: {result.DetectedPatterns.Count}");

            if (result.DetectedPatterns.Any())
            {
                parts.Add("Patrones de riesgo:");
                foreach (var pattern in result.DetectedPatterns.OrderByDescending(p => SeverityValue(p.Severity)))
                {
                    parts.Add($"  - [{pattern.Severity}] {pattern.Name}");
                }
            }

            if (result.HallucinationRisk == "alto")
            {
                parts.Add("⚠️ RECOMENDACIÓN: Respuesta requiere validación manual");
            }
            else if (result.HallucinationRisk == "medio")
            {
                parts.Add("ℹ️ RECOMENDACIÓN: Revisar respuesta con precaución");
            }

            return string.Join(" | ", parts);
        }

        private int SeverityValue(string severity) =>
            severity switch
            {
                "alto" => 3,
                "medio" => 2,
                "bajo" => 1,
                _ => 0
            };

        /// <summary>
        /// Extrae palabras clave de un texto.
        /// </summary>
        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "el", "la", "de", "que", "y", "es", "en", "a", "se", "por",
                "para", "con", "no", "una", "los", "su", "al", "lo", "como",
                "más", "fue", "del", "este", "fueron", "dos", "muy", "nos"
            };

            return Regex
                .Split(text, @"\W+")
                .Where(w => w.Length > 3 && !stopwords.Contains(w))
                .Take(10)
                .ToList();
        }
    }

    /// <summary>
    /// Resultado de validación de contexto.
    /// </summary>
    public class ContextValidationResult
    {
        public bool UsedContext { get; set; }
        public double Confidence { get; set; }  // 0.0 - 1.0
        public string HallucinationRisk { get; set; } = "bajo";  // bajo|medio|alto
        public string Diagnosis { get; set; } = "";
        public List<HallucinationPattern> DetectedPatterns { get; set; } = new();
        public int AvailableVectorCount { get; set; }
        public int ValidVectorCount { get; set; }
    }

    /// <summary>
    /// Patrón de alucinación detectado.
    /// </summary>
    public class HallucinationPattern
    {
        public string Name { get; set; } = "";
        public string Severity { get; set; } = "bajo";  // bajo|medio|alto
        public string Description { get; set; } = "";
        public string Evidence { get; set; } = "";
    }
}
