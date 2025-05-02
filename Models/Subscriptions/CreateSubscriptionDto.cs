using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;

namespace Voia.Api.Models.Subscriptions
{
    public class CreateSubscriptionDto
    {
        // ID del usuario al que se le va a asignar la suscripción
        public int UserId { get; set; }

        // ID del plan de suscripción que se le asigna al usuario
        public int PlanId { get; set; }

        // Fecha de inicio de la suscripción
        public DateTime StartedAt { get; set; }

        // Fecha de expiración de la suscripción
        public DateTime ExpiresAt { get; set; }

        // Estado de la suscripción (por defecto "active")
        public string Status { get; set; } = "active";
    }
}