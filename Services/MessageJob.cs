using System;

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
    }
}
