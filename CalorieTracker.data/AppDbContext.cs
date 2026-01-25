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

            // Configure the relationship as optional
            modelBuilder.Entity<MealEntry>()
                .HasOne(m => m.Food)
                .WithMany()
                .HasForeignKey(m => m.FoodId)
                .OnDelete(DeleteBehavior.Restrict) // or NoAction
                .IsRequired(false); // Mark as optional

            modelBuilder.Entity<WeightLog>()
                .HasIndex(w => w.LogDate);

            base.OnModelCreating(modelBuilder);
        }
    }
}
