using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Voia.Api.Models.Plans;
using Voia.Api.Models.Subscriptions; // ✅ Este using es necesario


namespace Voia.Api.Models
{
public class User
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    [Column("role_id")] 
    public int RoleId { get; set; }


    public int? DocumentTypeId { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string DocumentNumber { get; set; }
    public string DocumentPhotoUrl { get; set; }
    public string AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; }  
    
    public ICollection<Subscription> Subscriptions { get; set; }

    }

}
