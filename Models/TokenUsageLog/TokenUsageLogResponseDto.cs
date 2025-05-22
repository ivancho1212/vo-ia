using System;

namespace Voia.Api.Models.DTOs
{
    public class TokenUsageLogResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BotId { get; set; }
        public int TokensUsed { get; set; }
        public DateTime? UsageDate { get; set; }
    }
}
