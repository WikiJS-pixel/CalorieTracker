using CalorieTracker.data.DTOs;
using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    // Facade service that combines common operations
    public interface ICalorieTrackerService
    {
        Task<DailySummaryWithGoals> GetTodaySummaryWithGoalsAsync();
        Task<DailySummaryWithGoals> GetDateSummaryWithGoalsAsync(DateTime date);
        Task<DailySummary> GetDailySummaryAsync(DateTime date);
        Task<MealEntry> LogMealAsync(Food food, double amountGrams, MealType mealType, string? notes = null);
        // Progress tracking (needs both meal and weight data)
        Task<UserProgress> GetUserProgressAsync(DateTime startDate, DateTime endDate);
    }
}
