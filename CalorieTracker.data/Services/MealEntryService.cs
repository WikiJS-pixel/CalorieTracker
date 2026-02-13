using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class MealEntryService : IMealEntryService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<MealEntryService> _logger;
        private readonly IDatabaseLock _dbLock;

        public MealEntryService(
        IDbContextFactory dbContextFactory,
        ILogger<MealEntryService> logger,
        IDatabaseLock dbLock)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _dbLock = dbLock;
        }

        public async Task<MealEntry> AddMealEntryAsync(MealEntry entry)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                // Validate food exists
                var food = await context.Foods
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == entry.FoodId);

                if (food == null)
                    throw new KeyNotFoundException($"Food with ID {entry.FoodId} not found");

                if (food.IsDeleted)
                    throw new InvalidOperationException($"Food '{food.Name}' is deleted and cannot be used");

                entry.EntryDate = DateTime.UtcNow;
                await context.MealEntries.AddAsync(entry);
                await context.SaveChangesAsync();

                _logger.LogInformation("Added meal entry for food: {FoodName}", food.Name);
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding meal entry");
                throw;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<MealEntry?> GetMealEntryByIdAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.MealEntries
                    .Include(m => m.Food)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entry by ID: {Id}", id);
                return null;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> UpdateMealEntryAsync(MealEntry entry)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var existingEntry = await context.MealEntries
                    .FirstOrDefaultAsync(m => m.Id == entry.Id);

                if (existingEntry == null)
                    return false;

                // Update only allowed properties
                existingEntry.AmountGrams = entry.AmountGrams;
                existingEntry.MealType = entry.MealType;
                existingEntry.Notes = entry.Notes;

                // Only update food if changed
                if (existingEntry.FoodId != entry.FoodId)
                {
                    var food = await context.Foods
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(f => f.Id == entry.FoodId);

                    if (food == null || food.IsDeleted)
                        return false;

                    existingEntry.FoodId = entry.FoodId;
                }

                context.MealEntries.Update(existingEntry);
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated meal entry ID: {Id}", entry.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meal entry ID: {Id}", entry.Id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> DeleteMealEntryAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var entry = await context.MealEntries
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (entry == null)
                    return false;

                context.MealEntries.Remove(entry);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted meal entry ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting meal entry ID: {Id}", id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<MealEntry>> GetMealEntriesByDateAsync(DateTime date)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                return await context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.EntryDate >= startDate && m.EntryDate <= endDate)
                    .OrderBy(m => m.MealType)
                    .ThenBy(m => m.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entries for date: {Date}", date);
                return new List<MealEntry>();
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<MealEntry>> GetMealEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.EntryDate >= startDate && m.EntryDate <= endDate)
                    .OrderByDescending(m => m.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal entries for date range: {Start} to {End}",
                    startDate, endDate);
                return new List<MealEntry>();
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<MealEntry>> GetTodayMealEntriesAsync()
        {
            return await GetMealEntriesByDateAsync(DateTime.UtcNow.Date);
        }

        public async Task<DailySummary> GetDailySummaryAsync(DateTime date)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();

                var entries = await context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.EntryDate.Date == date)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var totals = CalculateNutritionTotals(entries);

                // Calculate per-meal calories (ensuring all MealTypes exist with at least 0)
                var mealCalories = Enum.GetValues(typeof(MealType))
                    .Cast<MealType>()
                    .ToDictionary(t => t, _ => 0.0);

                foreach (var group in entries.GroupBy(e => e.MealType))
                {
                    mealCalories[group.Key] = group.Sum(e => e.Food?.CaloriesForAmount(e.AmountGrams) ?? 0);
                }

                // We'll get target calories from GoalCalculationService later
                var summary = new DailySummary
                {
                    Date = date.Date,
                    TotalCalories = totals.Calories,
                    TotalProtein = totals.Protein,
                    TotalCarbs = totals.Carbs,
                    TotalFat = totals.Fat,
                    MealCount = entries.Count,
                    TargetCalories = 0, // Filled later by CalorieTrackerService
                    TargetProtein = 0,  // Filled later by CalorieTrackerService
                    MealCalories = mealCalories
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily summary for date: {Date}", date);
                return new DailySummary
                {
                    Date = date.Date,
                    MealCalories = Enum.GetValues(typeof(MealType))
                        .Cast<MealType>()
                        .ToDictionary(t => t, _ => 0.0)
                };
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<DailySummary> GetTodaySummaryAsync()
        {
            return await GetDailySummaryAsync(DateTime.UtcNow.Date);
        }

        public async Task<NutritionTotals> GetNutritionTotalsAsync(DateTime startDate, DateTime endDate)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var entries = await context.MealEntries
                    .Include(m => m.Food)
                    .Where(m => m.EntryDate >= startDate && m.EntryDate <= endDate)
                    .ToListAsync();

                return CalculateNutritionTotals(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nutrition totals for range: {Start} to {End}",
                    startDate, endDate);
                return new NutritionTotals();
            }
            finally
            {
                _dbLock.Semaphore.Release();
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
