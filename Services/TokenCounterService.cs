using System;
using System.Text.RegularExpressions;

namespace Api.Services
{
    public class TokenCounterService
    {
        /// <summary>
        /// Cuenta tokens en un texto usando una heurística simple:
        /// - Separa por espacios y signos de puntuación.
        /// - Cada palabra/puntuación se considera un token.
        /// </summary>
        /// <param name="text">Texto a procesar</param>
        /// <returns>Número de tokens</returns>
        public int CountTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Regex para separar palabras, números y símbolos
            var tokens = Regex.Matches(text, @"\w+|[^\s\w]");

            return tokens.Count;
        }

        /// <summary>
        /// Estima el costo o uso de tokens (ejemplo: para planes de usuario).
        /// </summary>
        /// <param name="text">Texto a analizar</param>
        /// <param name="tokenPrice">Costo por token (opcional)</param>
        /// <returns>Tupla con cantidad de tokens y costo</returns>
        public (int Tokens, decimal Cost) CountTokensWithCost(string text, decimal tokenPrice = 0.0m)
        {
            int tokens = CountTokens(text);
            decimal cost = tokens * tokenPrice;
            return (tokens, cost);
        }
    }
}
