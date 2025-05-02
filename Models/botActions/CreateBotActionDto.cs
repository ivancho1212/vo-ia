namespace Voia.Api.Models.BotActions
{
    public class CreateBotActionDto
    {
        public int BotId { get; set; }
        public string? TriggerPhrase { get; set; }
        public string ActionType { get; set; }
        public string? Payload { get; set; }
    }
}
