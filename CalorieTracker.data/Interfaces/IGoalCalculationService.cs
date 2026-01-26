using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IGoalCalculationService
    {
        // BMR Calculations (choose formula)
        double CalculateBMR_HarrisBenedict(double weightKg, double heightCm, int age, Gender gender);
        double CalculateBMR_MifflinStJeor(double weightKg, double heightCm, int age, Gender gender);

        // TDEE Calculations
        double CalculateTDEE(double bmr, ActivityLevel activityLevel);
        double CalculateTDEE_Multipliers(double bmr, ActivityLevel activityLevel);

        // Goal Calculations
        Task<DailyGoals> CalculateDailyGoalsAsync();
        Task<DailyGoals> CalculateDailyGoalsForUserAsync(UserProfile user);

        // Adjustment for weight goal
        double AdjustCaloriesForGoal(double tdee, WeightGoal goal, double weightChangeRateKgPerWeek);

        // Macronutrient Calculations
        DailyGoals CalculateMacronutrients(double targetCalories, UserSettings settings);
    }
}
