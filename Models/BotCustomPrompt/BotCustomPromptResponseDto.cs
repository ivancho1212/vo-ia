using System;

namespace Voia.Api.Models
{
    public class BotCustomPromptResponseDto
{
    public int Id { get; set; }
    public int? BotTemplateId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public int? TemplateTrainingSessionId { get; set; }
    public int BotId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

}
