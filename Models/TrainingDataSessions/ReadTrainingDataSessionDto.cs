using System;

namespace Voia.Api.Models.TrainingDataSessions
{
    public class ReadTrainingDataSessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? BotId { get; set; }
        public string? DataSummary { get; set; }
        public string? DataType { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
