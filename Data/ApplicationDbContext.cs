using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;

namespace Voia.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Bot> Bots { get; set; }

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

            // Relación entre User y Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users) // Asegúrate de que la propiedad Users esté en el modelo Role
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- MAPEO CAMPOS Bot ---
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

            // Relación entre Bot y User
            modelBuilder.Entity<Bot>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
