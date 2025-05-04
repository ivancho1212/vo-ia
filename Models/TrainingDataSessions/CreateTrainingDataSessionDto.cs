namespace Voia.Api.Models.TrainingDataSessions
{
    public class CreateTrainingDataSessionDto
    {
        public int UserId { get; set; }
        public int? BotId { get; set; }
        public string? DataSummary { get; set; }
        public string? DataType { get; set; } = "text";
        public string? Status { get; set; } = "pending";
    }
}
