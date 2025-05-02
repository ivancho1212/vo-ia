using System;

namespace Voia.Api.Models.GeneratedImages
{
    public class GeneratedImageDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? BotId { get; set; }
        public string Prompt { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
