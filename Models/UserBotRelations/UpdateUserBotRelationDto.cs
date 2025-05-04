namespace Voia.Api.Models.UserBotRelations
{
    public class UpdateUserBotRelationDto
    {
        public string RelationshipType { get; set; }
        public int? InteractionScore { get; set; }
        public DateTime? LastInteraction { get; set; }
    }
}
