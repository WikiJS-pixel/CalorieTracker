using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
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
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<DatabaseService> _logger;
        private readonly IFoodDataSeeder _foodDataSeeder;
        private readonly IDatabaseLock _dbLock;

        public DatabaseService(
            IDbContextFactory dbContextFactory,
            ILogger<DatabaseService> logger,
            IFoodDataSeeder foodDataSeeder,
            IDatabaseLock dbLock)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _foodDataSeeder = foodDataSeeder;
            _dbLock = dbLock;
        }


        public async Task InitializeAsync()
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
#if DEBUG
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(appDataPath, "calorietracker.db");
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    _logger.LogInformation("Deleted existing database for fresh start (debug only)");
                }
#endif

                // Create the context AFTER the potential debug delete
                using var context = _dbContextFactory.CreateContext();

                // 1. Apply migrations
                await context.Database.MigrateAsync();

                // 2. Seed default user profile and settings
                await SeedUserProfileAsync(context);
                await SeedUserSettingsAsync(context);

                // 3. Seed initial data (foods from JSON)
                await SeedFoodDataAsync(context);

                // 4. Seed initial weight log directly (avoid calling locked WeightService)
                await SeedInitialWeightLogAsync(context);

                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        private async Task SeedUserProfileAsync(AppDbContext context)
        {
            if (!await context.UserProfiles.AnyAsync())
            {
                var defaultProfile = new UserProfile
                {
                    Name = "Default User",
                    HasCompletedWizard = false,
                    BirthDate = new DateTime(1990, 1, 1),
                    Gender = Gender.Other,
                    HeightCm = 170,
                    ActivityLevel = ActivityLevel.ModeratelyActive,
                    WeightGoal = WeightGoal.Maintain,
                    WeightChangeRateKgPerWeek = 0.5,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = null
                };

                await context.UserProfiles.AddAsync(defaultProfile);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded default user profile");
            }
        }

        private async Task SeedUserSettingsAsync(AppDbContext context)
        {
            if (!await context.UserSettings.AnyAsync())
            {
                var defaultSettings = new UserSettings
                {
                    ProteinPercentage = 25,
                    CarbsPercentage = 50,
                    FatPercentage = 25,
                    UseMetricSystem = true,
                    TrackMacros = true,
                    TrackWater = false,
                    EnableMealReminders = true,
                    MealReminderTime = new TimeSpan(12, 0, 0)
                };

                await context.UserSettings.AddAsync(defaultSettings);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded default user settings");
            }
        }

        private async Task SeedFoodDataAsync(AppDbContext context)
        {
            // Check if Foods table is empty
            var foodCount = await context.Foods.CountAsync();
            _logger.LogInformation($"Current food count in database (including deleted): {foodCount}");

            if (!await context.Foods.AnyAsync(f => !f.IsDeleted))
            {
                _logger.LogInformation("No non-deleted foods found. Seeding food data...");
                try
                {
                    // Read JSON from MAUI app package
                    var foods = await _foodDataSeeder.GetSeedFoodsAsync();

                    _logger.LogInformation($"Food seeder returned {foods?.Count ?? 0} foods");

                    if (foods != null && foods.Any())
                    {
                        await context.Foods.AddRangeAsync(foods);
                        var result = await context.SaveChangesAsync();
                        _logger.LogInformation($"Saved {result} foods to database. Total foods now: {await context.Foods.CountAsync()}");
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

                        await context.Foods.AddAsync(emergencyFood);
                        await context.SaveChangesAsync();
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

                    await context.Foods.AddAsync(fallbackFood);
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Added fallback food due to seeding error");
                }
            }
            else
            {
                _logger.LogInformation("Foods already exist in database, skipping seed");
            }
        }

        private async Task SeedInitialWeightLogAsync(AppDbContext context)
        {
            if (!await context.WeightLogs.AnyAsync())
            {
                var initialLog = new WeightLog
                {
                    WeightKg = 70.0,
                    LogDate = DateTime.UtcNow,
                    Notes = "Initial weight"
                };

                await context.WeightLogs.AddAsync(initialLog);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded initial weight log");
            }
        }
    }
}
