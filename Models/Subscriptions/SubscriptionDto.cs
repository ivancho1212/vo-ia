namespace Voia.Api.Models.Subscriptions
{
    public class SubscriptionDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }

        public int PlanId { get; set; }
        public string PlanName { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public string Status { get; set; }
    }
}
