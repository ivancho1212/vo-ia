using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Voia.Api.Services.Chat
{
    public class BotDataCaptureService
    {
        private readonly ApplicationDbContext _context;
        private readonly DataExtractionService _dataExtractionService;

        public BotDataCaptureService(ApplicationDbContext context, DataExtractionService dataExtractionService)
        {
            _context = context;
            _dataExtractionService = dataExtractionService;
        }

        public async Task<List<BotDataSubmission>> ProcessMessageAsync(
            int botId,
            int? userId,
            string sessionId,
            string userMessage,
            List<DataField> currentFields
        )

        {
            var updatedFields = new List<DataField>(currentFields);
            var pendingFields = currentFields.Where(f => string.IsNullOrEmpty(f.Value)).ToList();

            if (!pendingFields.Any())
            {
                return new List<BotDataSubmission>();
            }

            // --- ESTRATEGIA HÍBRIDA ---

            // 1. Intento de captura con Regex (rápido y barato)
            var extractedData = new Dictionary<string, string>();
            foreach (var field in pendingFields)
            {
                var fieldNorm = Normalize(field.FieldName);
                var patterns = new List<string>
                {
                    $@"(?:mi\s+)?{fieldNorm}\s*(?:es|:|=)\s*([a-zA-Z0-9\s@.-]+)", // "nombre es...", "email: ..."
                };

                if (fieldNorm.Contains("nombre")) patterns.Add(@"(?:soy|me\s+llamo)\s+([a-zA-Z\s]+)");

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(userMessage, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        var value = match.Groups[1].Value.Trim();
                        if (ValidateFieldValue(value, field))
                        {
                            extractedData[field.FieldName] = value;
                            break; // Campo encontrado, pasar al siguiente
                        }
                    }
                }
            }

            // 2. Si Regex no encontró nada, usar IA como respaldo.
            //    También se podría llamar a la IA para los campos que Regex no encontró.
            var remainingPendingFields = pendingFields.Where(f => !extractedData.ContainsKey(f.FieldName)).ToList();
            if (!extractedData.Any() && remainingPendingFields.Any())
            {
                var aiExtractedData = await _dataExtractionService.ExtractDataWithAiAsync(botId, userMessage, remainingPendingFields);
                foreach (var item in aiExtractedData)
                {
                    extractedData[item.Key] = item.Value;
                }
            }

            // --- Fin de la estrategia híbrida ---

            if (extractedData.Any())
            {
                foreach (var extractedItem in extractedData)
                {
                    var fieldToUpdate = updatedFields.FirstOrDefault(f => f.FieldName.Equals(extractedItem.Key, StringComparison.OrdinalIgnoreCase));
                    if (fieldToUpdate != null && string.IsNullOrEmpty(fieldToUpdate.Value))
                    {
                        if (ValidateFieldValue(extractedItem.Value, fieldToUpdate))
                        {
                            fieldToUpdate.Value = extractedItem.Value;
                        }
                    }
                }
            }

            var newSubmissions = new List<BotDataSubmission>();
            foreach (var field in updatedFields)
            {
                var originalField = currentFields.FirstOrDefault(f => f.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase));
                if (field.Value != null && (originalField == null || originalField.Value != field.Value))
                {
                    var fieldDefinition = await _context.BotDataCaptureFields
                        .FirstOrDefaultAsync(f => f.BotId == botId && f.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase));

                    if (fieldDefinition != null)
                    {
                        var submission = new BotDataSubmission
                        {
                            BotId = botId,
                            CaptureFieldId = fieldDefinition.Id,
                            SubmissionValue = field.Value,
                            UserId = userId,
                            SubmissionSessionId = sessionId,
                            SubmittedAt = DateTime.UtcNow
                        };
                        newSubmissions.Add(submission);
                    }
                }
            }

            if (newSubmissions.Any())
            {
                _context.BotDataSubmissions.AddRange(newSubmissions);
                await _context.SaveChangesAsync();
            }

            return newSubmissions;
        }

        private string Normalize(string input) =>
            new string(input.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray())
                .ToLowerInvariant();

        private string Clean(string input) => Regex.Replace(input.Trim(), @"\s+", " ");

        private bool IsConfirmationOrGreeting(string input)
        {
            var patterns = new[] { "si", "no", "ok", "vale", "gracias", "de acuerdo", "hola", "buenos dias" };
            return patterns.Any(p => Normalize(input).Contains(p));
        }

        private bool ValidateFieldValue(string value, DataField field)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            // ✅ FIX: Usar solo FieldName ya que FieldType no existe en DataField.
            var type = (field.FieldName ?? "").ToLowerInvariant();

            if (type.Contains("telefono") || type.Contains("phone"))
            {
                return Regex.Matches(value, @"\d").Count >= 7; // ✅ FIX: La lógica ya era correcta.
            }

            return value.Length > 2;
        }
    }
}
