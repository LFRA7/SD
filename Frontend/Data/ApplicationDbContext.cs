using Microsoft.EntityFrameworkCore;
using Frontend.Models;

namespace Frontend.Data
{
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

            modelBuilder.Entity<SensorData>(entity =>
            {
                entity.ToTable("SensorData");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<SensorDataProcessed>(entity =>
            {
                entity.ToTable("SensorDataProcessed");
                entity.HasKey(e => e.Id);
            });
        }
    }
} 