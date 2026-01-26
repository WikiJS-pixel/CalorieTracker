using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IUserProfileService
    {
        // User Profile
        Task<UserProfile> GetUserProfileAsync();
        Task<UserProfile> UpdateUserProfileAsync(UserProfile profile);
        Task<int> GetAgeAsync();

        // User Settings
        Task<UserSettings> GetUserSettingsAsync();
        Task<UserSettings> UpdateUserSettingsAsync(UserSettings settings);
    }
}
