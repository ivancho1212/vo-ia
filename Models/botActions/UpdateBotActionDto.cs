namespace Voia.Api.Models.BotActions
{
    public class UpdateBotActionDto
    {
        public string? TriggerPhrase { get; set; }
        public string? ActionType { get; set; }
        public string? Payload { get; set; }
    }
}
