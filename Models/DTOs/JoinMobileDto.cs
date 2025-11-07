using System;

namespace Voia.Api.Models.DTOs
{
    public class JoinMobileDto
    {
        public string? DeviceType { get; set; } = "mobile";
        public string? UserAgent { get; set; }
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    }
}
