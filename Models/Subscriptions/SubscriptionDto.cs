public class SubscriptionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }

    public int PlanId { get; set; }
    public string PlanName { get; set; }
    public string PlanDescription { get; set; }
    public decimal PlanPrice { get; set; }
    public int PlanMaxTokens { get; set; }
    public int PlanBotsLimit { get; set; }
    public bool PlanIsActive { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; }
}
