using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;

namespace Voia.Api.Models.Subscriptions
{
public class UpdateSubscriptionDto
{
    public int? UserId { get; set; }
    public int? PlanId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; }
}

}
