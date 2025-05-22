namespace Voia.Api.Models.BotTrainingSession
{
    public class BotTrainingSessionCreateDto
    {
        public int BotId { get; set; }
        public string? SessionName { get; set; }
        public string? Description { get; set; }
    }
}
