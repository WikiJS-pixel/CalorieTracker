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

                // 2. Seed default user profile and settings
                await SeedUserProfileAsync();
                await SeedUserSettingsAsync();

                // 3. Seed initial data (foods from JSON)
                await SeedFoodDataAsync();

                // 4. Seed weight log for single user
                await SeedInitialWeightLogAsync();

                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private async Task SeedUserProfileAsync()
        {
            if (!await _context.UserProfiles.AnyAsync())
            {
                var defaultProfile = new UserProfile
                {
                    Name = "Default User",
                    BirthDate = new DateTime(1990, 1, 1),
                    Gender = Gender.Other,
                    HeightCm = 170,
                    ActivityLevel = ActivityLevel.ModeratelyActive,
                    WeightGoal = WeightGoal.Maintain,
                    WeightChangeRateKgPerWeek = 0.5,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = null
                };

                await _context.UserProfiles.AddAsync(defaultProfile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded default user profile");
            }
        }

        private async Task SeedUserSettingsAsync()
        {
            if (!await _context.UserSettings.AnyAsync())
            {
                var defaultSettings = new UserSettings
                {
                    ProteinPercentage = 25,
                    CarbsPercentage = 50,
                    FatPercentage = 25,
                    UseMetricSystem = true,
                    Theme = "Light",
                    TrackMacros = true,
                    TrackWater = false,
                    EnableMealReminders = true,
                    MealReminderTime = new TimeSpan(12, 0, 0)
                };

                await _context.UserSettings.AddAsync(defaultSettings);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded default user settings");
            }
        }

        private async Task SeedFoodDataAsync()
        {
            // Check if Foods table is empty
            var foodCount = await _context.Foods.CountAsync();
            _logger.LogInformation($"Current food count in database (including deleted): {foodCount}");

            if (!await _context.Foods.AnyAsync(f => !f.IsDeleted))
            {
                _logger.LogInformation("No non-deleted foods found. Seeding food data...");
                try
                {
                    // Read JSON from MAUI app package
                    var foods = await _foodDataSeeder.GetSeedFoodsAsync();

                    _logger.LogInformation($"Food seeder returned {foods?.Count ?? 0} foods");

                    if (foods != null && foods.Any())
                    {
                        await _context.Foods.AddRangeAsync(foods);
                        var result = await _context.SaveChangesAsync();
                        _logger.LogInformation($"Saved {result} foods to database. Total foods now: {await _context.Foods.CountAsync()}");
                    }
                    else
                    {
                        _logger.LogWarning("No food data to seed or food list is empty");

                        // Add emergency default food
                        var emergencyFood = new Food
                        {
                            Name = "Emergency Default Food",
                            CaloriesPer100g = 100,
                            ProteinPer100g = 10,
                            CarbsPer100g = 20,
                            FatPer100g = 5,
                            Description = "Added because seed failed"
                        };

                        await _context.Foods.AddAsync(emergencyFood);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Added default sample food");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error seeding food data");

                    // Add a fallback food item
                    var fallbackFood = new Food
                    {
                        Name = "Fallback Food",
                        CaloriesPer100g = 150,
                        ProteinPer100g = 15,
                        CarbsPer100g = 10,
                        FatPer100g = 8,
                        Description = "Fallback food due to seeding error"
                    };

                    await _context.Foods.AddAsync(fallbackFood);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added fallback food due to seeding error");
                }
            }
            else
            {
                _logger.LogInformation("Foods already exist in database, skipping seed");
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
