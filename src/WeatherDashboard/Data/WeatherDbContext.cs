using Microsoft.EntityFrameworkCore;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options)
        {
        }

        public DbSet<WeatherRecord> WeatherRecords { get; set; }
        public DbSet<SavedLocation> SavedLocations { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure WeatherRecord
            modelBuilder.Entity<WeatherRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Temperature).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.IconCode).HasMaxLength(10);

                entity.HasIndex(e => new { e.LocationId, e.Timestamp });

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.WeatherRecords)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SavedLocation
            modelBuilder.Entity<SavedLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.HasIndex(e => e.Name);
            });

            // Configure UserSetting
            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Value).HasMaxLength(500);
                entity.HasIndex(e => e.Key).IsUnique();
            });

            // Seed default settings
            modelBuilder.Entity<UserSetting>().HasData(
                new UserSetting
                {
                    Id = 1,
                    Key = "TemperatureUnit",
                    Value = "Celsius",
                    LastModified = DateTime.Now
                },
                new UserSetting
                {
                    Id = 2,
                    Key = "RefreshInterval",
                    Value = "30",
                    LastModified = DateTime.Now
                }
            );
        }
    }
}