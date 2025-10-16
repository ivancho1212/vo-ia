namespace Voia.Api.Models.bot
{
    public class BotDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; } // Optional
        public bool IsReady { get; set; } // Nuevo: indica si el bot está listo
    }
}