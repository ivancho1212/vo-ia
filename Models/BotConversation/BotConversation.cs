using System;
using Microsoft.AspNetCore.Mvc;
using Voia.Api.Models.BotConversation;
using Voia.Api.Services.Interfaces;
using Voia.Api.Data;



namespace Voia.Api.Models.BotConversation
{
    public class BotConversation
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
