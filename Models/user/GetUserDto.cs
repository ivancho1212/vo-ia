using Voia.Api.Models.DTOs;
using Voia.Api.Models.Subscriptions;

public class GetUserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string DocumentNumber { get; set; }
    public string DocumentPhotoUrl { get; set; }
    public string AvatarUrl { get; set; }
    public string PlanName { get; set; }  // O puedes incluir detalles más completos de 'Plan'
    public SubscriptionDto? Subscription { get; set; }

    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Role information (solo los detalles del rol)
    public RoleDto Role { get; set; }

    // Si deseas incluir más detalles del plan en el DTO:
    public PlanDto Plan { get; set; }  // Detalles completos del plan, no solo el nombre
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
