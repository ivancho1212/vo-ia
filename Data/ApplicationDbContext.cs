using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;

namespace Voia.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Bot> Bots => Set<Bot>();
        // Agrega aquí los demás DbSet conforme los vayas creando
    }
}
