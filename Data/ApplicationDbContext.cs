using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.Prompts;
using Voia.Api.Models.Plans;
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.SupportTicket; 
using Voia.Api.Models.BotProfiles;
using Voia.Api.Models.AiModelConfigs;
using Voia.Api.Models.TrainingDataSessions;
using Voia.Api.Models.GeneratedImages;
using Voia.Api.Models.UserPreferences;
using Voia.Api.Models.UserBotRelations;
using Voia.Api.Models.BotActions;



namespace Voia.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Bot> Bots { get; set; }
        public DbSet<BotStyle> BotStyles { get; set; }
        public DbSet<Prompt> Prompts { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportResponse> SupportResponses { get; set; }
        public DbSet<BotProfile> BotProfiles { get; set; }
        public DbSet<AiModelConfig> AiModelConfigs { get; set; }
        public DbSet<TrainingDataSession> TrainingDataSessions { get; set; }
        public DbSet<GeneratedImage> GeneratedImages { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<UserBotRelation> UserBotRelations { get; set; }
        public DbSet<BotAction> BotActions { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- MAPEO CAMPOS User ---
            modelBuilder.Entity<User>()
                .Property(u => u.DocumentNumber)
                .HasColumnName("document_number");

            modelBuilder.Entity<User>()
                .Property(u => u.DocumentTypeId)
                .HasColumnName("document_type_id");

            modelBuilder.Entity<User>()
                .Property(u => u.Phone)
                .HasColumnName("phone");

            modelBuilder.Entity<User>()
                .Property(u => u.Address)
                .HasColumnName("address");

            modelBuilder.Entity<User>()
                .Property(u => u.IsVerified)
                .HasColumnName("is_verified");

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<User>()
                .Property(u => u.AvatarUrl)
                .HasColumnName("avatar_url");

            modelBuilder.Entity<User>()
                .Property(u => u.DocumentPhotoUrl)
                .HasColumnName("document_photo_url");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users) 
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bot>()
                .Property(b => b.UserId)
                .HasColumnName("user_id");

            modelBuilder.Entity<Bot>()
                .Property(b => b.Name)
                .HasColumnName("name");

            modelBuilder.Entity<Bot>()
                .Property(b => b.Description)
                .HasColumnName("description");

            modelBuilder.Entity<Bot>()
                .Property(b => b.ApiKey)
                .HasColumnName("api_key");

            modelBuilder.Entity<Bot>()
                .Property(b => b.ModelUsed)
                .HasColumnName("model_used")
                .HasMaxLength(255);

            modelBuilder.Entity<Bot>()
                .Property(b => b.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            modelBuilder.Entity<Bot>()
                .Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Bot>()
                .Property(b => b.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<Bot>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BotStyle>(entity =>
{
            entity.ToTable("bot_styles"); // Aquí se indica el nombre real de la tabla

            // Ahora mapeamos cada propiedad al nombre real de columna en la base de datos
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BotId).HasColumnName("bot_id");
            entity.Property(e => e.Theme).HasColumnName("theme");
            entity.Property(e => e.PrimaryColor).HasColumnName("primary_color");
            entity.Property(e => e.SecondaryColor).HasColumnName("secondary_color");
            entity.Property(e => e.FontFamily).HasColumnName("font_family");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.CustomCss).HasColumnName("custom_css");
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate(); // Esto es para que respete el valor automático
        });

        }
        
    }
}
