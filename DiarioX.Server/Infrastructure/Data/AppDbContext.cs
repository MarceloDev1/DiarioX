using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar todas as configurações do assembly
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
