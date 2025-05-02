namespace Voia.Api.Models.UserBotRelations
{
    public class CreateUserBotRelationDto
    {
        public int UserId { get; set; }
        public int BotId { get; set; }
        public string RelationshipType { get; set; } = "otro";
        public int? InteractionScore { get; set; } = 0;
        public DateTime? LastInteraction { get; set; }
    }
}
