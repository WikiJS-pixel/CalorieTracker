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

        public UserProfileService(AppDbContext context, ILogger<UserProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserProfile> GetUserProfileAsync()
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Include(p => p.WeightLogs.OrderByDescending(w => w.LogDate).Take(10))
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

        public async Task<double?> GetCurrentWeightAsync()
        {
            var latestLog = await GetLatestWeightLogAsync();
            return latestLog?.WeightKg;
        }

        public async Task<WeightLog> LogWeightAsync(double weightKg, string? notes = null)
        {
            try
            {
                var weightLog = new WeightLog
                {
                    UserProfileId = 1,
                    WeightKg = weightKg,
                    LogDate = DateTime.UtcNow,
                    Notes = notes
                };

                await _context.WeightLogs.AddAsync(weightLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Logged weight: {Weight}kg", weightKg);
                return weightLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging weight");
                throw;
            }
        }

        public async Task<List<WeightLog>> GetWeightHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.WeightLogs.Where(w => w.UserProfileId == 1);

                if (startDate.HasValue)
                    query = query.Where(w => w.LogDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(w => w.LogDate <= endDate.Value);

                return await query.OrderByDescending(w => w.LogDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight history");
                throw;
            }
        }

        public async Task<WeightLog?> GetLatestWeightLogAsync()
        {
            try
            {
                return await _context.WeightLogs
                    .Where(w => w.UserProfileId == 1)
                    .OrderByDescending(w => w.LogDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest weight log");
                return null;
            }
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
                        UserProfileId = 1,
                        ProteinPercentage = 25,
                        CarbsPercentage = 50,
                        FatPercentage = 25,
                        UseMetricSystem = true,
                        Theme = "Light",
                        TrackMacros = true,
                        EnableMealReminders = false,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.UserSettings.AddAsync(settings);
                    await _context.SaveChangesAsync();
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
                settings.UserProfileId = 1;
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
