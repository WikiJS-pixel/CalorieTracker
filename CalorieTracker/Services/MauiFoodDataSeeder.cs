using System.Text.Json;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Services
{
    public class MauiFoodDataSeeder : IFoodDataSeeder
    {
        private readonly ILogger<MauiFoodDataSeeder> _logger;

        // Add constructor with logger
        public MauiFoodDataSeeder(ILogger<MauiFoodDataSeeder> logger)
        {
            _logger = logger;
        }

        public async Task<List<Food>> GetSeedFoodsAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Attempting to load foods.json from app package...");
                // Check if file exists in app package
                var fileName = "foods.json";

                // Use MAUI's asset loading pattern
                await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

                if (stream == null)
                {
                    _logger.LogError("FileSystem.OpenAppPackageFileAsync returned null for foods.json");
                    return GetDefaultFoods();
                }

                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync(ct);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("foods.json is empty.");
                    return GetDefaultFoods();
                }

                var foods = JsonSerializer.Deserialize<List<Food>>(json) ?? new List<Food>();

                _logger.LogInformation($"Deserialized {foods.Count} foods from JSON");

                // Validate we got data
                if (!foods.Any())
                {
                    _logger.LogWarning("No food data loaded from seed file. Using default foods.");
                    return GetDefaultFoods();
                }

                _logger.LogInformation($"Loaded {foods.Count} foods from foods.json");
                return foods;
            }
            catch (FileNotFoundException ex)
            {
                _logger?.LogError(ex, "Foods.json not found in app package");
                return GetDefaultFoods();
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to parse foods.json");
                return GetDefaultFoods();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error loading seed foods");
                return GetDefaultFoods();
            }
        }

        private List<Food> GetDefaultFoods()
        {
            _logger.LogInformation("Returning default foods");

            return new List<Food>
            {
                new Food
                {
                    Name = "Apple",
                    CaloriesPer100g = 52,
                    ProteinPer100g = 0.3,
                    CarbsPer100g = 14,
                    FatPer100g = 0.2,
                    Description = "Fresh red apple"
                },
                new Food
                {
                    Name = "Banana",
                    CaloriesPer100g = 89,
                    ProteinPer100g = 1.1,
                    CarbsPer100g = 23,
                    FatPer100g = 0.3,
                    Description = "Medium banana"
                },
                new Food
                {
                    Name = "Chicken Breast",
                    CaloriesPer100g = 165,
                    ProteinPer100g = 31,
                    CarbsPer100g = 0,
                    FatPer100g = 3.6,
                    Description = "Boneless, skinless chicken breast"
                },
                new Food
                {
                    Name = "White Rice",
                    CaloriesPer100g = 130,
                    ProteinPer100g = 2.7,
                    CarbsPer100g = 28,
                    FatPer100g = 0.3,
                    Description = "Cooked white rice"
                },
                new Food
                {
                    Name = "Broccoli",
                    CaloriesPer100g = 34,
                    ProteinPer100g = 2.8,
                    CarbsPer100g = 7,
                    FatPer100g = 0.4,
                    Description = "Steamed broccoli"
                }
            };
        }
    }
}
