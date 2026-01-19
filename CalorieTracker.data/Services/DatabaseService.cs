using CalorieTracker.data.Interfaces;
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
        private readonly IWeightService _weightService;

        public DatabaseService(
            AppDbContext context,
            ILogger<DatabaseService> logger,
            IFoodDataSeeder foodDataSeeder,
            IWeightService weightService)
        {
            _context = context;
            _logger = logger;
            _foodDataSeeder = foodDataSeeder;
            _weightService = weightService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 1. Apply migrations
                await _context.Database.MigrateAsync();

                // 2. Seed initial data (foods from JSON)
                await SeedFoodDataAsync();

                // 3. Seed weight log for single user
                await SeedInitialWeightLogAsync();

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
        }

        private async Task SeedInitialWeightLogAsync()
        {
            var currentWeight = await _weightService.GetCurrentWeightAsync();
            // Seed initial weight log if none exists
            if (currentWeight == null)
            {
                await _weightService.AddWeightLogAsync(new WeightLog
                {
                    WeightKg = 70.0,
                    LogDate = DateTime.UtcNow,
                    Notes = "Initial weight"
                });

                _logger.LogInformation("Seeded initial weight log");
            }
        }
    }
}
