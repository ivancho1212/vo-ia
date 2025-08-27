using System;
using System.Collections.Generic;

namespace Voia.Api.Dtos.Bot
{
    public class BotDataGroupedSubmissionDto
    {
    public int? UserId { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, List<string>> Values { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    }
}
