using System.ComponentModel.DataAnnotations;
namespace Voia.Api.DTOs
{
    public class UpdateSupportResponseDto
    {
        public int? ResponderId { get; set; }
        public string Message { get; set; }
    }
}
