using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Voia.Api.Data;
using Voia.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Voia.Api.Services.Chat
{
    public class DataCaptureResult
    {
        public List<BotDataSubmission> NewSubmissions { get; set; } = new List<BotDataSubmission>();
        public string ConfirmationPrompt { get; set; }
        public string AiClarificationPrompt { get; set; }
        public bool RequiresAiClarification { get; set; }
    }

    public class BotDataCaptureService
    {
        private readonly ApplicationDbContext _context;
        private readonly DataExtractionService _dataExtractionService;

        public BotDataCaptureService(ApplicationDbContext context, DataExtractionService dataExtractionService)
        {
            _context = context;
            _dataExtractionService = dataExtractionService;
        }

        public async Task<DataCaptureResult> ProcessMessageAsync(
            int botId,
            int? userId,
            string sessionId,
            string userMessage,
            List<DataField> currentFields
        )
        {
            var result = new DataCaptureResult();
            var pendingFields = currentFields.Where(f => string.IsNullOrEmpty(f.Value)).ToList();

            if (!pendingFields.Any())
            {
                return result;
            }

            var extractedData = await ExtractDataWithHybridStrategyAsync(botId, userMessage, pendingFields);

            if (!extractedData.Any())
            {
                return result;
            }

            var ambiguityCheck = CheckForAmbiguities(extractedData, pendingFields);
            if (ambiguityCheck.IsAmbiguous)
            {
                result.RequiresAiClarification = true;
                result.AiClarificationPrompt = BuildClarificationPrompt(userMessage, ambiguityCheck.AmbiguousFields, pendingFields);
                return result; // Devolver para que la IA pida aclaración
            }

            var newSubmissions = await SaveNewDataAsync(botId, userId, sessionId, extractedData, currentFields);
            if (newSubmissions.Any())
            {
                result.NewSubmissions.AddRange(newSubmissions);
                var updatedFields = ApplyExtractedData(currentFields, extractedData);
                result.ConfirmationPrompt = BuildConfirmationPrompt(userMessage, newSubmissions, updatedFields);
            }

            return result;
        }

        private async Task<Dictionary<string, string>> ExtractDataWithHybridStrategyAsync(int botId, string userMessage, List<DataField> pendingFields)
        {
            var extractedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var aiExtractedData = await _dataExtractionService.ExtractDataWithAiAsync(botId, userMessage, pendingFields);
                foreach (var item in aiExtractedData)
                {
                    extractedData[item.Key] = item.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotDataCaptureService] ⚠️ Advertencia: La extracción con IA falló. {{ex.Message}}");
            }

            if (!extractedData.Any())
            {
                foreach (var field in pendingFields)
                {
                    var fieldNorm = Normalize(field.FieldName);
                    var patterns = new List<string>
        {
            $@"(?:mi\\s+)?{fieldNorm}\\s*(?:es|el|:|=)\\s*([a-zA-Z0-9\\s@.-]+)"
        };

                    if (fieldNorm.Contains("nombre")) patterns.Add(@"(?:soy|me\\s+llamo)\\s+([a-zA-Z\\s]+)");

                    foreach (var pattern in patterns)
                    {
                        var match = Regex.Match(userMessage, pattern, RegexOptions.IgnoreCase);
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            extractedData[field.FieldName] = match.Groups[1].Value.Trim();
                            break;
                        }
                    }
                }
            }

            return extractedData;
        }

        private (bool IsAmbiguous, Dictionary<string, string> AmbiguousFields) CheckForAmbiguities(Dictionary<string, string> extractedData, List<DataField> pendingFields)
        {
            var ambiguousFields = new Dictionary<string, string>();
            if (extractedData.Count == 1)
            {
                var singleExtraction = extractedData.First();
                var otherFieldKeywords = pendingFields
                    .Where(p => !p.FieldName.Equals(singleExtraction.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(p => Normalize(p.FieldName));

                if (otherFieldKeywords.Any(kw => Normalize(singleExtraction.Value).Contains(kw)))
                {
                    ambiguousFields.Add(singleExtraction.Key, singleExtraction.Value);
                }
            }

            return (ambiguousFields.Any(), ambiguousFields);
        }

        private string BuildClarificationPrompt(string userMessage, Dictionary<string, string> ambiguousFields, List<DataField> pendingFields)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("The user's message seems to contain multiple pieces of information mixed together. I need you to ask for clarification in a natural and friendly way.");
            promptBuilder.AppendLine($"User's original message: \"{userMessage}\"");

            foreach (var field in ambiguousFields)
            {
                promptBuilder.AppendLine($"I tentatively extracted \"{field.Value}\" for the field \"{field.Key}\", but this seems to contain other data.");
            }

            var pendingFieldNames = pendingFields.Select(f => f.FieldName).ToList();
            promptBuilder.AppendLine($"Please ask the user to provide the following details clearly, perhaps one by one: {string.Join(", ", pendingFieldNames)}.");
            promptBuilder.AppendLine("For example, you could say: 'Thanks! To make sure I have everything right, could you give me your full name first?'");

            return promptBuilder.ToString();
        }

        private async Task<List<BotDataSubmission>> SaveNewDataAsync(int botId, int? userId, string sessionId, Dictionary<string, string> extractedData, List<DataField> currentFields)
        {
            var newSubmissions = new List<BotDataSubmission>();
            foreach (var item in extractedData)
            {
                var fieldDefinition = await _context.BotDataCaptureFields
                    .FirstOrDefaultAsync(f => f.BotId == botId && f.FieldName.Equals(item.Key, StringComparison.OrdinalIgnoreCase));

                if (fieldDefinition != null && IsNewOrUpdated(currentFields, item.Key, item.Value))
                {
                    var submission = new BotDataSubmission
                    {
                        BotId = botId,
                        CaptureFieldId = fieldDefinition.Id,
                        SubmissionValue = item.Value,
                        UserId = userId,
                        SubmissionSessionId = sessionId,
                        SubmittedAt = DateTime.UtcNow,
                        // Dejar campos contextuales nulos por defecto; pueden ser rellenados por el llamador (por ejemplo ChatHub)
                        ConversationId = null,
                        CaptureIntent = null,
                        CaptureSource = "extraction",
                        MetadataJson = null
                    };
                    newSubmissions.Add(submission);
                }
            }

            if (newSubmissions.Any())
            {
                _context.BotDataSubmissions.AddRange(newSubmissions);
                await _context.SaveChangesAsync();
            }
            return newSubmissions;
        }

        private string BuildConfirmationPrompt(string userMessage, List<BotDataSubmission> newSubmissions, List<DataField> updatedFields)
        {
            var promptBuilder = new StringBuilder();
            var savedFields = newSubmissions.Select(s => _context.BotDataCaptureFields.Find(s.CaptureFieldId).FieldName).ToList();

            promptBuilder.AppendLine("I have just saved the following information for the user:");
            foreach (var fieldName in savedFields)
            {
                var value = updatedFields.First(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).Value;
                promptBuilder.AppendLine($"- {fieldName}: {value}");
            }

            var remainingFields = updatedFields.Where(f => string.IsNullOrEmpty(f.Value)).Select(f => f.FieldName).ToList();
            if (remainingFields.Any())
            {
                promptBuilder.AppendLine($"Please confirm the saved data to the user in a friendly tone and then ask for the next piece of information from this list: {string.Join(", ", remainingFields)}.");
            }
            else
            {
                promptBuilder.AppendLine("All required data has been collected. Please confirm all the saved data to the user and then answer their original question based on their message.");
                promptBuilder.AppendLine($"Original user message: \"{userMessage}\"");
            }

            return promptBuilder.ToString();
        }

        private List<DataField> ApplyExtractedData(List<DataField> currentFields, Dictionary<string, string> extractedData)
        {
            var updatedList = new List<DataField>(currentFields);
            foreach (var item in extractedData)
            {
                var field = updatedList.FirstOrDefault(f => f.FieldName.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
                if (field != null && string.IsNullOrEmpty(field.Value))
                {
                    field.Value = item.Value;
                }
            }
            return updatedList;
        }

        private bool IsNewOrUpdated(List<DataField> currentFields, string fieldName, string newValue)
        {
            var field = currentFields.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            return field != null && field.Value != newValue;
        }

        private string Normalize(string input)
        {
            return input.ToLower().Trim();
        }
        private bool ValidateFieldValue(string value, DataField field)
        {
            if (string.IsNullOrWhiteSpace(value) || field == null)
            {
                return false;
            }

            var type = (field.FieldName ?? "").ToLowerInvariant();

            if (type.Contains("telefono") || type.Contains("phone"))
            {
                return Regex.Matches(value, @"\d").Count >= 7;
            }

            return value.Length > 2;
        }
    }
}
