using CalorieTracker.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Food> Foods => Set<Food>();
        public DbSet<MealEntry> MealEntries => Set<MealEntry>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<WeightLog> WeightLogs => Set<WeightLog>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Only configure Food -> MealEntry relationship (for soft delete)
            modelBuilder.Entity<Food>()
                .HasQueryFilter(f => !f.IsDeleted);

            // Add indexes for performance
            modelBuilder.Entity<MealEntry>()
                .HasIndex(m => m.EntryDate);

            modelBuilder.Entity<WeightLog>()
                .HasIndex(w => w.LogDate);

            // Ensure only one UserProfile exists
            modelBuilder.Entity<UserProfile>()
                .HasData(new UserProfile
                {
                    Id = 1,
                    Name = "Default User",
                    BirthDate = new DateTime(1990, 1, 1),
                    Gender = Gender.Other,
                    HeightCm = 170,
                    ActivityLevel = ActivityLevel.ModeratelyActive,
                    WeightGoal = WeightGoal.Maintain,
                    WeightChangeRateKgPerWeek = 0.5
                });

            // Seed single user settings
            modelBuilder.Entity<UserSettings>()
                .HasData(new UserSettings
                {
                    Id = 1,
                    ProteinPercentage = 25,
                    CarbsPercentage = 50,
                    FatPercentage = 25,
                    UseMetricSystem = true,
                    Theme = "Light",
                    TrackMacros = true,
                    TrackWater = false,
                    EnableMealReminders = true,
                    MealReminderTime = new TimeSpan(12, 0, 0)
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
