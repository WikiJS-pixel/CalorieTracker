using CalorieTracker.Data;
using CalorieTracker.Data.Interfaces;
using CalorieTracker.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class MealEntryService : IMealEntryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MealEntryService> _logger;

        public MealEntryService(AppDbContext context, ILogger<MealEntryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MealEntry> AddMealEntryAsync(MealEntry entry)
        {
            try
            {
                // Validate food exists
                var food = await _context.Foods
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == entry.FoodId);

                if (food == null)
                    throw new KeyNotFoundException($"Food with ID {entry.FoodId} not found");

                if (food.IsDeleted)
                    throw new InvalidOperationException($"Food '{food.Name}' is deleted and cannot be used");

                // Ensure user profile exists
                entry.UserProfileId = 1;
                entry.EntryDate = DateTime.UtcNow;

                await _context.MealEntries.AddAsync(entry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added meal entry for food: {FoodName}", food.Name);
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding meal entry");
                throw;
            }
        }

        public async Task<MealEntry?> GetMealEntryByIdAsync(int id)
        {
            try
            {
                return await _context.MealEntries
                    .Include(m => m.Food)
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entry by ID: {Id}", id);
                return null;
            }
        }

        public async Task<bool> UpdateMealEntryAsync(MealEntry entry)
        {
            try
            {
                var existingEntry = await _context.MealEntries
                    .FirstOrDefaultAsync(m => m.Id == entry.Id && m.UserProfileId == 1);

                if (existingEntry == null)
                    return false;

                // Update only allowed properties
                existingEntry.AmountGrams = entry.AmountGrams;
                existingEntry.MealType = entry.MealType;
                existingEntry.Notes = entry.Notes;

                // Only update food if changed
                if (existingEntry.FoodId != entry.FoodId)
                {
                    var food = await _context.Foods
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(f => f.Id == entry.FoodId);

                    if (food == null || food.IsDeleted)
                        return false;

                    existingEntry.FoodId = entry.FoodId;
                }

                _context.MealEntries.Update(existingEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated meal entry ID: {Id}", entry.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meal entry ID: {Id}", entry.Id);
                return false;
            }
        }

        public async Task<bool> DeleteMealEntryAsync(int id)
        {
            try
            {
                var entry = await _context.MealEntries
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == 1);

                if (entry == null)
                    return false;

                _context.MealEntries.Remove(entry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted meal entry ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting meal entry ID: {Id}", id);
                return false;
            }
        }

        public async Task<List<MealEntry>> GetMealEntriesByDateAsync(DateTime date)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                return await _context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.UserProfileId == 1 &&
                               m.EntryDate >= startDate &&
                               m.EntryDate <= endDate)
                    .OrderBy(m => m.MealType)
                    .ThenBy(m => m.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entries for date: {Date}", date);
                return new List<MealEntry>();
            }
        }

        public async Task<List<MealEntry>> GetMealEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.UserProfileId == 1 &&
                               m.EntryDate >= startDate &&
                               m.EntryDate <= endDate)
                    .OrderByDescending(m => m.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entries for date range: {Start} to {End}",
                    startDate, endDate);
                return new List<MealEntry>();
            }
        }

        public async Task<List<MealEntry>> GetTodayMealEntriesAsync()
        {
            return await GetMealEntriesByDateAsync(DateTime.UtcNow.Date);
        }

        public async Task<DailySummary> GetDailySummaryAsync(DateTime date)
        {
            try
            {
                var entries = await GetMealEntriesByDateAsync(date);
                var totals = CalculateNutritionTotals(entries);

                // We'll get target calories from GoalCalculationService later
                var summary = new DailySummary
                {
                    Date = date.Date,
                    TotalCalories = totals.Calories,
                    TotalProtein = totals.Protein,
                    TotalCarbs = totals.Carbs,
                    TotalFat = totals.Fat,
                    MealCount = entries.Count,
                    TargetCalories = 0, // Will be populated by caller
                    TargetProtein = 0   // Will be populated by caller
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily summary for date: {Date}", date);
                return new DailySummary { Date = date.Date };
            }
        }

        public async Task<DailySummary> GetTodaySummaryAsync()
        {
            return await GetDailySummaryAsync(DateTime.UtcNow.Date);
        }

        public async Task<NutritionTotals> GetNutritionTotalsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var entries = await GetMealEntriesByDateRangeAsync(startDate, endDate);
                return CalculateNutritionTotals(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nutrition totals for range: {Start} to {End}",
                    startDate, endDate);
                return new NutritionTotals();
            }
        }

        private NutritionTotals CalculateNutritionTotals(List<MealEntry> entries)
        {
            var totals = new NutritionTotals();

            foreach (var entry in entries)
            {
                if (entry.Food != null)
                {
                    totals.Calories += entry.Food.CaloriesForAmount(entry.AmountGrams);
                    totals.Protein += entry.Food.ProteinForAmount(entry.AmountGrams);
                    totals.Carbs += entry.Food.CarbsForAmount(entry.AmountGrams);
                    totals.Fat += entry.Food.FatForAmount(entry.AmountGrams);
                }
            }

            return totals;
        }
    }
}
