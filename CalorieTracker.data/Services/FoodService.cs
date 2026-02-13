using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class FoodService : IFoodService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<FoodService> _logger;
        private readonly IDatabaseLock _dbLock;

        public FoodService(
            IDbContextFactory dbContextFactory,
            IDatabaseLock dbLock,
            ILogger<FoodService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _dbLock = dbLock;
            _logger = logger;
        }

        public async Task<Food?> GetFoodByIdAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.Foods
                    .IgnoreQueryFilters() // Include soft-deleted if needed
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting food by ID: {Id}", id);
                return null;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<Food>> SearchFoodsAsync(string searchTerm, bool includeDeleted = false)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var query = context.Foods.AsQueryable();

                if (!includeDeleted)
                {
                    query = query.IgnoreQueryFilters().Where(f => !f.IsDeleted);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(f =>
                        EF.Functions.Like(f.Name, $"%{searchTerm}%") ||
                        (f.Description != null && EF.Functions.Like(f.Description, $"%{searchTerm}%"))
                    );
                }

                return await query
                    .OrderBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching foods with term: {Term}", searchTerm);
                return new List<Food>();
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<Food> AddFoodAsync(Food food)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                food.IsDeleted = false; // Ensure new food is not deleted

                await context.Foods.AddAsync(food);
                await context.SaveChangesAsync();

                _logger.LogInformation("Added new food: {Name} (ID: {Id})", food.Name, food.Id);
                return food;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding food: {Name}", food.Name);
                throw;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<Food> UpdateFoodAsync(Food food)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var existingFood = await context.Foods
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == food.Id);

                if (existingFood == null)
                    throw new KeyNotFoundException($"Food with ID {food.Id} not found");

                // Update properties
                existingFood.Name = food.Name;
                existingFood.CaloriesPer100g = food.CaloriesPer100g;
                existingFood.ProteinPer100g = food.ProteinPer100g;
                existingFood.CarbsPer100g = food.CarbsPer100g;
                existingFood.FatPer100g = food.FatPer100g;
                existingFood.Description = food.Description;

                context.Foods.Update(existingFood);
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated food: {Name} (ID: {Id})", food.Name, food.Id);
                return existingFood;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating food ID: {Id}", food.Id);
                throw;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> SoftDeleteFoodAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var food = await context.Foods.FindAsync(id);
                if (food == null)
                    return false;

                food.IsDeleted = true;
                context.Foods.Update(food);
                await context.SaveChangesAsync();

                _logger.LogInformation("Soft-deleted food ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting food ID: {Id}", id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> RestoreFoodAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var food = await context.Foods
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsDeleted)
                    .ConfigureAwait(false);

                if (food == null)
                    return false;

                food.IsDeleted = false;
                context.Foods.Update(food);
                await context.SaveChangesAsync();

                _logger.LogInformation("Restored food ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring food ID: {Id}", id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }
    }
}
