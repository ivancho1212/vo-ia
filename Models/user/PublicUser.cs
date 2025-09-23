using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Messages;

namespace Voia.Api.Models.Users
{
    [Table("public_users")]
    public class PublicUser
    {
        [Key]
        public int Id { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("country")]
        public string? Country { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("bot_id")]
        public int? BotId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Relaciones
        public virtual Bot? Bot { get; set; }
        public virtual ICollection<Conversation> Conversations { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
    }
}
