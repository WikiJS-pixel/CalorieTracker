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
            base.OnModelCreating(modelBuilder);

            // Configure soft delete filter for Food
            modelBuilder.Entity<Food>()
                .HasQueryFilter(f => !f.IsDeleted);

            // Configure relationships and indexes
            modelBuilder.Entity<MealEntry>()
                .HasOne(m => m.Food)
                .WithMany()
                .HasForeignKey(m => m.FoodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MealEntry>()
                .HasOne(m => m.UserProfile)
                .WithMany()
                .HasForeignKey(m => m.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeightLog>()
                .HasOne(w => w.UserProfile)
                .WithMany()
                .HasForeignKey(w => w.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSettings>()
                .HasOne(us => us.UserProfile)
                .WithOne()
                .HasForeignKey<UserSettings>(us => us.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance
            modelBuilder.Entity<MealEntry>()
                .HasIndex(m => m.EntryDate);

            modelBuilder.Entity<MealEntry>()
                .HasIndex(m => m.UserProfileId);

            modelBuilder.Entity<WeightLog>()
                .HasIndex(w => w.LogDate);

            modelBuilder.Entity<WeightLog>()
                .HasIndex(w => w.UserProfileId);

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
        }
    }
}
