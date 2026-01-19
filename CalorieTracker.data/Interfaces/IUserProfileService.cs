using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.Interfaces
{
    public interface IUserProfileService
    {
        // User Profile
        Task<UserProfile> GetUserProfileAsync();
        Task<UserProfile> UpdateUserProfileAsync(UserProfile profile);
        Task<int> GetAgeAsync();

        // Weight Management
        Task<double?> GetCurrentWeightAsync();
        Task<WeightLog> LogWeightAsync(double weightKg, string? notes = null);
        Task<List<WeightLog>> GetWeightHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<WeightLog?> GetLatestWeightLogAsync();

        // User Settings
        Task<UserSettings> GetUserSettingsAsync();
        Task<UserSettings> UpdateUserSettingsAsync(UserSettings settings);
    }
}
