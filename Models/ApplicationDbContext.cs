using Microsoft.EntityFrameworkCore;

namespace Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorData> SensorData { get; set; }
    public DbSet<SensorDataProcessed> SensorDataProcessed { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SensorData>()
            .HasIndex(s => new { s.Topic, s.Processed });

        modelBuilder.Entity<SensorDataProcessed>()
            .HasIndex(s => s.Topic);
    }
} 