using CalorieTracker.Data.DTOs;
using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.Interfaces
{
    // Facade service that combines common operations
    public interface ICalorieTrackerService
    {
        Task<DailySummaryWithGoals> GetTodaySummaryWithGoalsAsync();
        Task<DailySummaryWithGoals> GetDateSummaryWithGoalsAsync(DateTime date);
        Task<MealEntry> LogMealAsync(Food food, double amountGrams, MealType mealType, string? notes = null);
        Task<UserProgress> GetUserProgressAsync(DateTime startDate, DateTime endDate);
    }
}
