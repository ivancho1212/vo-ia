namespace Voia.Api.Models.BotIntegrations
{
    public class UpdateBotIntegrationDto
    {
        public string? IntegrationType { get; set; }
        public string? AllowedDomain { get; set; }
        public string? ApiToken { get; set; }
    }
}
