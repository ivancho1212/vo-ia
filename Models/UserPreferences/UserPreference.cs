using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.UserPreferences
{
    [Table("user_preferences")]
    public class UserPreference
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("interest_id")]
        public int InterestId { get; set; }
    }
}
