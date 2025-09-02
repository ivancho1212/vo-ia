using System.Text.RegularExpressions;
using Voia.Api.Data;
using Voia.Api.Models;

namespace Voia.Api.Services.Chat
{
    public class BotDataCaptureService
    {
        private readonly ApplicationDbContext _context;

        public BotDataCaptureService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BotDataSubmission>> ProcessMessageAsync(
            int botId,
            int? userId,
            string sessionId,
            string message
        )
        {
            // 1. Obtener campos configurados para el bot
            var fields = _context.BotDataCaptureFields
                .Where(f => f.BotId == botId)
                .ToList();

            var newSubmissions = new List<BotDataSubmission>();

            // 2. Buscar en el mensaje
            foreach (var field in fields)
            {
                // Omitir si ya existe submission previa
                var alreadyCaptured = _context.BotDataSubmissions.Any(s =>
                    s.BotId == botId &&
                    s.CaptureFieldId == field.Id &&
                    (s.UserId == userId || s.SubmissionSessionId == sessionId)
                );

                if (alreadyCaptured) continue;

                string? value = ExtractValue(field.FieldName, message);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var submission = new BotDataSubmission
                    {
                        BotId = botId,
                        CaptureFieldId = field.Id,
                        SubmissionValue = value,
                        UserId = userId,
                        SubmissionSessionId = sessionId,
                        SubmittedAt = DateTime.UtcNow
                    };

                    _context.BotDataSubmissions.Add(submission);
                    newSubmissions.Add(submission);
                }
            }

            if (newSubmissions.Any())
                await _context.SaveChangesAsync();

            return newSubmissions;
        }

        private string? ExtractValue(string fieldName, string text)
        {
            text = text.ToLower();

            switch (fieldName.ToLower())
            {
                case "nombre":
                    var matchNombre = Regex.Match(text, @"(?:me llamo|mi nombre es|yo soy)\s+([a-záéíóúñ ]+)");
                    return matchNombre.Success ? matchNombre.Groups[1].Value.Trim() : null;

                case "direccion":
                    var matchDir = Regex.Match(text, @"(?:mi direccion es|vivo en)\s+([a-z0-9áéíóúñ ,.-]+)");
                    return matchDir.Success ? matchDir.Groups[1].Value.Trim() : null;

                case "pais":
                    var matchPais = Regex.Match(text, @"(?:soy de|vengo de|mi pais es)\s+([a-záéíóúñ ]+)");
                    return matchPais.Success ? matchPais.Groups[1].Value.Trim() : null;

                default:
                    return null;
            }
        }
    }
}
