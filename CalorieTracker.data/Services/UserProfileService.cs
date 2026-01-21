using CalorieTracker.data.Interfaces;
using CalorieTracker.Data;
using CalorieTracker.Data.Interfaces;
using CalorieTracker.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IWeightService _weightService;

        public UserProfileService(AppDbContext context, ILogger<UserProfileService> logger,
            IWeightService weightService)
        {
            _context = context;
            _logger = logger;
            _weightService = weightService;
        }

        public async Task<UserProfile> GetUserProfileAsync()
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.Id == 1);

                if (profile == null)
                {
                    // Create default profile if none exists
                    profile = new UserProfile
                    {
                        Id = 1,
                        Name = "New User",
                        BirthDate = DateTime.UtcNow.AddYears(-30),
                        Gender = Gender.Other,
                        HeightCm = 170,
                        ActivityLevel = ActivityLevel.ModeratelyActive,
                        WeightGoal = WeightGoal.Maintain,
                        WeightChangeRateKgPerWeek = 0.5,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.UserProfiles.AddAsync(profile);
                    await _context.SaveChangesAsync();
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                throw;
            }
        }

        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile profile)
        {
            try
            {
                // Ensure we're always updating the singleton profile
                profile.Id = 1;
                profile.LastUpdatedDate = DateTime.UtcNow;

                _context.UserProfiles.Update(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user profile");
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                throw;
            }
        }

        public async Task<int> GetAgeAsync()
        {
            var profile = await GetUserProfileAsync();
            var today = DateTime.UtcNow;
            var age = today.Year - profile.BirthDate.Year;

            // Adjust if birthday hasn't occurred this year
            if (profile.BirthDate.Date > today.AddYears(-age))
                age--;

            return age;
        }

        public async Task<UserSettings> GetUserSettingsAsync()
        {
            try
            {
                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.Id == 1);

                if (settings == null)
                {
                    // Create default settings
                    settings = new UserSettings
                    {
                        Id = 1,
                        ProteinPercentage = 25,
                        CarbsPercentage = 50,
                        FatPercentage = 25,
                        UseMetricSystem = true,
                        Theme = "Light",
                        TrackMacros = true,
                        EnableMealReminders = false,
                        MealReminderTime = new TimeSpan(12, 0, 0),
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.UserSettings.AddAsync(settings);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created default user settings");
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user settings");
                throw;
            }
        }

        public async Task<UserSettings> UpdateUserSettingsAsync(UserSettings settings)
        {
            // Validate percentages are reasonable
            if (settings.ProteinPercentage < 0 || settings.CarbsPercentage < 0 || settings.FatPercentage < 0)
                throw new ArgumentException("Percentages cannot be negative");
            try
            {
                // Ensure we're always updating the singleton settings
                settings.Id = 1;
                settings.LastUpdatedDate = DateTime.UtcNow;

                // Validate percentages sum to 100 (or close)
                var total = settings.ProteinPercentage + settings.CarbsPercentage + settings.FatPercentage;
                if (Math.Abs(total - 100) > 0.01)
                {
                    // Normalize to 100%
                    settings.ProteinPercentage = (settings.ProteinPercentage / total) * 100;
                    settings.CarbsPercentage = (settings.CarbsPercentage / total) * 100;
                    settings.FatPercentage = (settings.FatPercentage / total) * 100;
                }

                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user settings");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                throw;
            }
        }
    }
}
