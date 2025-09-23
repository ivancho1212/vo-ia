using Voia.Api.Models.DTOs;
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.bot;

public class GetUserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }

    // 🔹 Nuevos campos
    public string Country { get; set; }
    public string City { get; set; }

    public string Address { get; set; }
    public string DocumentNumber { get; set; }
    public string DocumentPhotoUrl { get; set; }
    public string AvatarUrl { get; set; }
    public string PlanName { get; set; }
    public SubscriptionDto? Subscription { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public RoleDto Role { get; set; }
    public PlanDto Plan { get; set; }
    public List<BotDto> Bots { get; set; } = new List<BotDto>();
}

public class PlanDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int MaxTokens { get; set; }
    public int? BotsLimit { get; set; }
}
