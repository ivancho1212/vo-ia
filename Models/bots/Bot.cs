using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.BotIntegrations; // Ajusta el namespace según donde esté el modelo BotIaProvider

namespace Voia.Api.Models
{
    public class Bot
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string ApiKey { get; set; }
        public string ModelUsed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } // Navegación

        // NUEVO: Propiedad FK para el proveedor de IA
        [Column("ia_provider_id")]
        public int IaProviderId { get; set; }

        // NUEVO: Propiedad de navegación
        [ForeignKey("IaProviderId")]
        public BotIaProvider IaProvider { get; set; }
    }
}
