using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Users;

namespace Voia.Api.Models.Conversations
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("bot_id")]
        public int BotId { get; set; }

        // ✅ Estas propiedades ahora permiten null para evitar errores de conversión
        public string? Title { get; set; }

        [Column("user_message")]
        public string? UserMessage { get; set; }

        [Column("bot_response")]
        public string? BotResponse { get; set; }

        [Column("status")]
        public string Status { get; set; } = "activa";
        [Column("is_with_ai")]
        public bool IsWithAI { get; set; } = true; // o false si prefieres por defecto

        [Column("blocked")]
        public bool Blocked { get; set; } = false;

        [Column("last_message")]
        public string? LastMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // ✅ PROPIEDAD AÑADIDA
        [Column("last_active_at")]
        public DateTime? LastActiveAt { get; set; }

        // 🆕 CAMPOS PARA MULTI-DISPOSITIVO (MOBILE SESSIONS)
        [Column("active_mobile_session")]
        public bool ActiveMobileSession { get; set; } = false;

        [Column("mobile_device_type")]
        public string? MobileDeviceType { get; set; } // "mobile", "tablet", etc

        [Column("mobile_joined_at")]
        public DateTime? MobileJoinedAt { get; set; }

        // 🆕 CAMPO PARA CIERRE POR EXPIRACIÓN
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        // Relaciones de navegación
        public virtual User? User { get; set; }
        public virtual Bot? Bot { get; set; }
        [Column("public_user_id")]
        public int? PublicUserId { get; set; }

        [ForeignKey("PublicUserId")]
        public virtual PublicUser? PublicUser { get; set; }

    }
}
