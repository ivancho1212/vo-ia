using System;

namespace Voia.Api.Models.DTOs
{
    public class BotTemplateResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int IaProviderId { get; set; }
        public int? DefaultStyleId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
