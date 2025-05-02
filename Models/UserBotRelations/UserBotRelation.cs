using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.UserBotRelations
{
    [Table("user_bot_relations")]
    public class UserBotRelation
    {
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("bot_id")]
        public int BotId { get; set; }
        [Column("relationship_type")]
        public string RelationshipType { get; set; } = "otro";
        [Column("interaction_score")]
        public int? InteractionScore { get; set; } = 0;
        [Column("last_interaction", TypeName = "datetime")]
        public DateTime? LastInteraction { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
