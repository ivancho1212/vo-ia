namespace Voia.Api.Models.BotIntegrations
{
    public class UpdateBotIntegrationDto
    {
        public string? IntegrationType { get; set; }
        public List<string>? AllowedDomains { get; set; } // Cambiado a array
        public string? ApiToken { get; set; }
    }
}