using CalorieTracker.Data.Interfaces;
using CalorieTracker.Data.Models;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class GoalCalculationService : IGoalCalculationService
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<GoalCalculationService> _logger;

        public GoalCalculationService(
            IUserProfileService userProfileService,
            ILogger<GoalCalculationService> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        public double CalculateBMR_HarrisBenedict(double weightKg, double heightCm, int age, Gender gender)
        {
            // Harris-Benedict Equation (Revised)
            // Weight in kg, height in cm, age in years

            if (gender == Gender.Male)
            {
                return 88.362 + (13.397 * weightKg) + (4.799 * heightCm) - (5.677 * age);
            }
            else if (gender == Gender.Female)
            {
                return 447.593 + (9.247 * weightKg) + (3.098 * heightCm) - (4.330 * age);
            }
            else
            {
                // Average of male and female for "Other"
                var maleBMR = 88.362 + (13.397 * weightKg) + (4.799 * heightCm) - (5.677 * age);
                var femaleBMR = 447.593 + (9.247 * weightKg) + (3.098 * heightCm) - (4.330 * age);
                return (maleBMR + femaleBMR) / 2;
            }
        }

        public double CalculateBMR_MifflinStJeor(double weightKg, double heightCm, int age, Gender gender)
        {
            // Mifflin-St Jeor Equation (more accurate for modern populations)

            if (gender == Gender.Male)
            {
                return (10 * weightKg) + (6.25 * heightCm) - (5 * age) + 5;
            }
            else if (gender == Gender.Female)
            {
                return (10 * weightKg) + (6.25 * heightCm) - (5 * age) - 161;
            }
            else
            {
                // Average of male and female for "Other"
                var maleBMR = (10 * weightKg) + (6.25 * heightCm) - (5 * age) + 5;
                var femaleBMR = (10 * weightKg) + (6.25 * heightCm) - (5 * age) - 161;
                return (maleBMR + femaleBMR) / 2;
            }
        }

        public double CalculateTDEE_Multipliers(double bmr, ActivityLevel activityLevel)
        {
            // Using standard multipliers based on activity level
            return activityLevel switch
            {
                ActivityLevel.Sedentary => bmr * 1.2,        // Little or no exercise
                ActivityLevel.LightlyActive => bmr * 1.375,  // Light exercise 1-3 days/week
                ActivityLevel.ModeratelyActive => bmr * 1.55,// Moderate exercise 3-5 days/week
                ActivityLevel.VeryActive => bmr * 1.725,     // Hard exercise 6-7 days/week
                ActivityLevel.ExtraActive => bmr * 1.9,      // Very hard exercise + physical job
                _ => bmr * 1.2
            };
        }

        public double CalculateTDEE(double bmr, ActivityLevel activityLevel)
        {
            return CalculateTDEE_Multipliers(bmr, activityLevel);
        }

        public double AdjustCaloriesForGoal(double tdee, WeightGoal goal, double weightChangeRateKgPerWeek)
        {
            // 1 kg of fat ≈ 7700 calories
            // Weekly calorie adjustment = weightChangeRate * 7700
            // Daily adjustment = weekly / 7

            var weeklyCalorieAdjustment = weightChangeRateKgPerWeek * 7700;
            var dailyAdjustment = weeklyCalorieAdjustment / 7;

            return goal switch
            {
                WeightGoal.Lose => tdee - dailyAdjustment,
                WeightGoal.Gain => tdee + dailyAdjustment,
                WeightGoal.Maintain => tdee,
                _ => tdee
            };
        }

        public DailyGoals CalculateMacronutrients(double targetCalories, UserSettings settings)
        {
            // Calculate grams based on percentages and calories
            // Protein: 4 calories per gram
            // Carbs: 4 calories per gram
            // Fat: 9 calories per gram

            var goals = new DailyGoals
            {
                ProteinPercentage = settings.ProteinPercentage,
                CarbsPercentage = settings.CarbsPercentage,
                FatPercentage = settings.FatPercentage,

                TargetCalories = targetCalories
            };

            // Ensure percentages sum to 100%
            var totalPercentage = goals.ProteinPercentage + goals.CarbsPercentage + goals.FatPercentage;
            if (Math.Abs(totalPercentage - 100) > 0.01)
            {
                goals.ProteinPercentage = (goals.ProteinPercentage / totalPercentage) * 100;
                goals.CarbsPercentage = (goals.CarbsPercentage / totalPercentage) * 100;
                goals.FatPercentage = (goals.FatPercentage / totalPercentage) * 100;
            }

            // Calculate grams
            goals.TargetProtein = (targetCalories * (goals.ProteinPercentage / 100)) / 4;
            goals.TargetCarbs = (targetCalories * (goals.CarbsPercentage / 100)) / 4;
            goals.TargetFat = (targetCalories * (goals.FatPercentage / 100)) / 9;

            return goals;
        }

        public async Task<DailyGoals> CalculateDailyGoalsAsync()
        {
            try
            {
                var userProfile = await _userProfileService.GetUserProfileAsync();
                return await CalculateDailyGoalsForUserAsync(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily goals");
                throw;
            }
        }

        public async Task<DailyGoals> CalculateDailyGoalsForUserAsync(UserProfile user)
        {
            try
            {
                // Get current weight
                var currentWeight = await _userProfileService.GetCurrentWeightAsync();
                if (!currentWeight.HasValue)
                {
                    throw new InvalidOperationException("Current weight not available");
                }

                // Get age
                var age = await _userProfileService.GetAgeAsync();

                // Get user settings for macro distribution
                var settings = await _userProfileService.GetUserSettingsAsync();

                // 1. Calculate BMR (using Mifflin-St Jeor as it's more accurate)
                var bmr = CalculateBMR_MifflinStJeor(
                    currentWeight.Value,
                    user.HeightCm,
                    age,
                    user.Gender
                );

                _logger.LogDebug("BMR calculated: {BMR} calories", bmr);

                // 2. Calculate TDEE
                var tdee = CalculateTDEE(bmr, user.ActivityLevel);
                _logger.LogDebug("TDEE calculated: {TDEE} calories", tdee);

                // 3. Adjust for weight goal
                var targetCalories = AdjustCaloriesForGoal(
                    tdee,
                    user.WeightGoal,
                    user.WeightChangeRateKgPerWeek
                );

                _logger.LogDebug("Target calories: {Target} (Goal: {Goal}, Rate: {Rate} kg/week)",
                    targetCalories, user.WeightGoal, user.WeightChangeRateKgPerWeek);

                // 4. Calculate macronutrient goals
                var goals = CalculateMacronutrients(targetCalories, settings);

                _logger.LogInformation("Daily goals calculated: {Calories} calories, {Protein}g protein, {Carbs}g carbs, {Fat}g fat",
                    goals.TargetCalories, goals.TargetProtein, goals.TargetCarbs, goals.TargetFat);

                return goals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily goals for user");
                throw;
            }
        }
    }
}
