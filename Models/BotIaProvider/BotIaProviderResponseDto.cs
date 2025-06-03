using System;

namespace Voia.Api.Models.DTOs
{
    public class BotIaProviderResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string Status { get; set; } // ✅ Nuevo campo
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
