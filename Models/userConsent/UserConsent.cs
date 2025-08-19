namespace Voia.Api.Models
{
    public class UserConsent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ConsentType { get; set; } = null!;
        public bool Granted { get; set; }
        public DateTime? GrantedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
