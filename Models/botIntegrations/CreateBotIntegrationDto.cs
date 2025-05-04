namespace Voia.Api.Models.BotIntegrations
{
    public class CreateBotIntegrationDto
    {
        public int BotId { get; set; }
        public string? IntegrationType { get; set; }
        public string? AllowedDomain { get; set; }
        public string? ApiToken { get; set; }
    }
}
