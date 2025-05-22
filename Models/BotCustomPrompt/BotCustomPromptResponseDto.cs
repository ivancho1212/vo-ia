// Models/BotCustomPrompt/BotCustomPromptResponseDto.cs
namespace Voia.Api.Models.DTOs
{
    public class BotCustomPromptResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public int? TrainingSessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
