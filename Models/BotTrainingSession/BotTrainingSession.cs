// Archivo: Voia.Api\Models\BotTrainingSession\BotTrainingSession.cs
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models;

namespace Voia.Api.Models.BotTrainingSession // 👈 OJO: Este es el namespace
{
    public class BotTrainingSession
    {
        public int Id { get; set; }
        public int BotId { get; set; }

        [ForeignKey("BotId")]
        public Bot Bot { get; set; }

        public string? SessionName { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
