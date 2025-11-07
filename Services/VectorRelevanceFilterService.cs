using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Voia.Api.Services
{
    /// <summary>
    /// ✅ FIX A: Servicio para filtrar vectores relevantes por similitud.
    /// 
    /// Características:
    /// - Filtra vectores con similarity_score >= minSimilarity (default 0.6)
    /// - Ordena descendentemente por relevancia
    /// - Maneja IDictionary y dynamic objects
    /// - Fallback: devuelve todos los vectores si ocurre error
    /// 
    /// Uso:
    ///     var service = new VectorRelevanceFilterService();
    ///     var filtered = service.FilterRelevantVectors(vectors, minSimilarity: 0.6);
    /// </summary>
    public class VectorRelevanceFilterService
    {
        /// <summary>
        /// Filtra vectores por relevancia y los ordena por score descendente.
        /// </summary>
        /// <param name="vectors">Lista de vectores (pueden ser dict, dynamic, objects)</param>
        /// <param name="minSimilarity">Umbral mínimo de similitud (0.0 - 1.0)</param>
        /// <returns>Vectores filtrados y ordenados por relevancia</returns>
        public List<object> FilterRelevantVectors(List<object> vectors, double minSimilarity = 0.6)
        {
            if (vectors == null || vectors.Count == 0)
            {
                return new List<object>();
            }

            try
            {
                var filtered = new List<(object vector, double score)>();

                foreach (var vector in vectors)
                {
                    double? similarity = ExtractSimilarityScore(vector);
                    
                    if (similarity.HasValue && similarity.Value >= minSimilarity)
                    {
                        filtered.Add((vector, similarity.Value));
                    }
                }

                // ✅ Ordenar descendentemente por relevancia
                var sorted = filtered
                    .OrderByDescending(x => x.score)
                    .Select(x => x.vector)
                    .ToList();

                return sorted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en FilterRelevantVectors: {ex.Message}");
                // Fallback: devolver todos los vectores
                return vectors;
            }
        }

        /// <summary>
        /// Extrae el score de similaridad de diferentes formatos de objetos.
        /// </summary>
        private double? ExtractSimilarityScore(object vector)
        {
            if (vector == null)
                return null;

            try
            {
                // ✅ Caso 1: IDictionary<string, object>
                if (vector is System.Collections.IDictionary dict)
                {
                    return GetScoreFromDictionary(dict);
                }

                // ✅ Caso 2: dynamic object
                dynamic dyn = vector;
                try
                {
                    object? scoreObj = dyn.similarity_score ?? dyn.score ?? dyn.Score;
                    if (scoreObj != null && double.TryParse(scoreObj.ToString(), out var score))
                    {
                        return score;
                    }
                }
                catch { /* ignore */ }

                // ✅ Caso 3: JsonElement
                if (vector is JsonElement elem)
                {
                    if (elem.ValueKind == JsonValueKind.Object)
                    {
                        if (elem.TryGetProperty("similarity_score", out var scoreProp))
                            return scoreProp.GetDouble();
                        if (elem.TryGetProperty("score", out scoreProp))
                            return scoreProp.GetDouble();
                    }
                }

                // ✅ Caso 4: Anonymous object / reflection
                var type = vector.GetType();
                var props = type.GetProperties();

                foreach (var prop in props.Where(p => 
                    p.Name.Equals("similarity_score", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals("score", StringComparison.OrdinalIgnoreCase)))
                {
                    var value = prop.GetValue(vector);
                    if (value != null && double.TryParse(value.ToString(), out var score))
                    {
                        return score;
                    }
                }
            }
            catch { /* ignore parsing errors */ }

            return null;
        }

        /// <summary>
        /// Extrae score de IDictionary.
        /// </summary>
        private double? GetScoreFromDictionary(System.Collections.IDictionary dict)
        {
            string[] scoreKeys = { "similarity_score", "score", "Score" };

            foreach (var key in scoreKeys)
            {
                if (dict.Contains(key))
                {
                    var value = dict[key];
                    if (value != null && double.TryParse(value.ToString(), out var score))
                    {
                        return score;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Valida que los vectores cumplan con el formato esperado.
        /// </summary>
        /// <param name="vectors">Vectores a validar</param>
        /// <returns>True si todos tienen al menos un similarity_score</returns>
        public bool ValidateVectors(List<object> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return false;

            return vectors.All(v => ExtractSimilarityScore(v).HasValue);
        }

        /// <summary>
        /// Obtiene estadísticas de los vectores.
        /// </summary>
        public (int total, int valid, double avgScore, double maxScore, double minScore) GetVectorStats(List<object> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return (0, 0, 0, 0, 0);

            var scores = vectors
                .Select(v => ExtractSimilarityScore(v))
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();

            return (
                total: vectors.Count,
                valid: scores.Count,
                avgScore: scores.Count > 0 ? scores.Average() : 0,
                maxScore: scores.Count > 0 ? scores.Max() : 0,
                minScore: scores.Count > 0 ? scores.Min() : 0
            );
        }
    }
}
