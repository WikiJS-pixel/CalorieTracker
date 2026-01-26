using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IMealEntryService
    {
        // CRUD Operations
        Task<MealEntry> AddMealEntryAsync(MealEntry entry);
        Task<MealEntry?> GetMealEntryByIdAsync(int id);
        Task<bool> UpdateMealEntryAsync(MealEntry entry);
        Task<bool> DeleteMealEntryAsync(int id);

        // Queries
        Task<List<MealEntry>> GetMealEntriesByDateAsync(DateTime date);
        Task<List<MealEntry>> GetMealEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<MealEntry>> GetTodayMealEntriesAsync();

        // Summaries
        Task<DailySummary> GetDailySummaryAsync(DateTime date);
        Task<DailySummary> GetTodaySummaryAsync();
        Task<NutritionTotals> GetNutritionTotalsAsync(DateTime startDate, DateTime endDate);
    }
}
