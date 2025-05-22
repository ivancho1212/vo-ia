namespace Voia.Api.Models.BotTrainingSession
{
    public class BotTrainingSessionResponseDto
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public string? SessionName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
