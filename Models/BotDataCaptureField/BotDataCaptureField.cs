using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("bot_data_capture_fields")]
    public class BotDataCaptureField
    {
        [Key]
        public int Id { get; set; }

        [Column("bot_id")]
        [Required]
        public int BotId { get; set; }

        [Column("field_name")]
        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; }

        [Column("field_type")]
        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; }

        [Column("is_required")]
        public bool? IsRequired { get; set; } = false;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
