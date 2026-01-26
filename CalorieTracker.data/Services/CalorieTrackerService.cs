using CalorieTracker.data.DTOs;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;

namespace CalorieTracker.data.Services
{
    public class CalorieTrackerService : ICalorieTrackerService
    {
        private readonly IMealEntryService _mealEntryService;
        private readonly IGoalCalculationService _goalCalculationService;
        private readonly IFoodService _foodService;
        private readonly IWeightService _weightService;

        public CalorieTrackerService(
            IMealEntryService mealEntryService,
            IGoalCalculationService goalCalculationService,
            IFoodService foodService,
            IWeightService weightService)
        {
            _mealEntryService = mealEntryService;
            _goalCalculationService = goalCalculationService;
            _foodService = foodService;
            _weightService = weightService;
        }

        public async Task<DailySummary> GetDailySummaryAsync(DateTime date)
        {
            var result = await GetDateSummaryWithGoalsAsync(date);
            return result.Summary;
        }

        public async Task<DailySummaryWithGoals> GetTodaySummaryWithGoalsAsync()
        {
            return await GetDateSummaryWithGoalsAsync(DateTime.UtcNow.Date);
        }

        public async Task<DailySummaryWithGoals> GetDateSummaryWithGoalsAsync(DateTime date)
        {
            var summary = await _mealEntryService.GetDailySummaryAsync(date);
            var goals = await _goalCalculationService.CalculateDailyGoalsAsync();

            // Update summary with goals
            summary.TargetCalories = goals.TargetCalories;
            summary.TargetProtein = goals.TargetProtein;

            return new DailySummaryWithGoals
            {
                Summary = summary,
                Goals = goals
            };
        }

        public async Task<MealEntry> LogMealAsync(Food food, double amountGrams, MealType mealType, string? notes = null)
        {
            ArgumentNullException.ThrowIfNull(food);

            if (amountGrams <= 0)
                throw new ArgumentException("Amount must be greater than 0", nameof(amountGrams));

            var entry = new MealEntry
            {
                FoodId = food.Id,
                AmountGrams = amountGrams,
                MealType = mealType,
                Notes = notes
            };

            return await _mealEntryService.AddMealEntryAsync(entry);
        }

        public async Task<UserProgress> GetUserProgressAsync(DateTime startDate, DateTime endDate)
        {
            var progress = new UserProgress
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get nutrition totals
            var totals = await _mealEntryService.GetNutritionTotalsAsync(startDate, endDate);
            progress.TotalCalories = totals.Calories;
            progress.TotalProtein = totals.Protein;

            // Get weight at start and end
            var startWeightLog = await _weightService.GetWeightLogByDateAsync(startDate);
            var endWeightLog = await _weightService.GetWeightLogByDateAsync(endDate);

            if (startWeightLog != null && endWeightLog != null)
            {
                progress.StartingWeight = startWeightLog.WeightKg;
                progress.EndingWeight = endWeightLog.WeightKg;
                progress.WeightChange = endWeightLog.WeightKg - startWeightLog.WeightKg;
            }

            // Calculate average daily calories
            var days = (endDate - startDate).TotalDays;
            if (days > 0)
            {
                progress.AverageDailyCalories = totals.Calories / days;
            }

            return progress;
        }
    }
}
