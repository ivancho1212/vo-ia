using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.Plans
{
    public class Plan
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0, 9999999999.99)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("max_tokens")]
        public int MaxTokens { get; set; }

        [Column("bots_limit")]
        public int? BotsLimit { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }
    }
}