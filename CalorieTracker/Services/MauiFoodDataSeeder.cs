using System.Text.Json;
using CalorieTracker.Data.Interfaces;
using CalorieTracker.Data.Models;
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
                // Use MAUI's asset loading pattern
                await using var stream = await FileSystem.OpenAppPackageFileAsync("foods.json");
                // OR use embedded resource approach for better cross-platform compatibility

                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync(ct);

                var foods = JsonSerializer.Deserialize<List<Food>>(json) ?? new List<Food>();

                // Validate we got data
                if (!foods.Any())
                {
                    _logger?.LogWarning("No food data loaded from seed file");
                }

                return foods;
            }
            catch (FileNotFoundException ex)
            {
                _logger?.LogError(ex, "Foods.json not found in app package");
                throw; // Re-throw - this is a critical failure
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to parse foods.json");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error loading seed foods");
                throw;
            }
        }
    }
}
