using CalorieTracker.Data;
using CalorieTracker.Data.Interfaces;
using CalorieTracker.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseService> _logger;
        private readonly IFoodDataSeeder _foodDataSeeder;

        public DatabaseService(
            AppDbContext context,
            ILogger<DatabaseService> logger,
            IFoodDataSeeder foodDataSeeder)
        {
            _context = context;
            _logger = logger;
            _foodDataSeeder = foodDataSeeder;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 1. Apply migrations
                await _context.Database.MigrateAsync();

                // 2. Seed initial data (foods from JSON)
                await SeedFoodDataAsync();

                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private async Task SeedFoodDataAsync()
        {
            // Check if Foods table is empty
            if (!await _context.Foods.AnyAsync())
            {
                try
                {
                    // Read JSON from MAUI app package
                    var foods = await _foodDataSeeder.GetSeedFoodsAsync();

                    if (foods != null && foods.Any())
                    {
                        await _context.Foods.AddRangeAsync(foods);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Seeded {foods.Count} foods");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error seeding food data");
                }
            }

            // Seed initial weight log if none exists
            if (!await _context.WeightLogs.AnyAsync())
            {
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync();
                if (userProfile != null)
                {
                    await _context.WeightLogs.AddAsync(new WeightLog
                    {
                        UserProfileId = userProfile.Id,
                        WeightKg = 70.0, // Default starting weight
                        LogDate = DateTime.UtcNow,
                        Notes = "Initial weight"
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
