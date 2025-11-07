using System;
using System.Collections.Generic;
using Voia.Api.Services;

namespace Voia.Api.Services
{
    public class MessageJob
    {
        public int ConversationId { get; set; }
        public int BotId { get; set; }
        public int? UserId { get; set; }
        public int MessageId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string TempId { get; set; } = string.Empty;
        
        // 🆕 Ubicación del usuario público para contextualizar respuestas
        public string? UserCountry { get; set; }
        public string? UserCity { get; set; }
        public string? ContextMessage { get; set; }
        
        // 🆕 Campos de captura de datos: estado actual de qué se ha capturado y qué falta
        public List<DataField>? CapturedFields { get; set; }
    }
}
