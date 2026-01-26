using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class FoodService : IFoodService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FoodService> _logger;

        public FoodService(AppDbContext context, ILogger<FoodService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Food?> GetFoodByIdAsync(int id)
        {
            try
            {
                return await _context.Foods
                    .IgnoreQueryFilters() // Include soft-deleted if needed
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting food by ID: {Id}", id);
                return null;
            }
        }

        public async Task<List<Food>> SearchFoodsAsync(string searchTerm, bool includeDeleted = false)
        {
            try
            {
                var query = _context.Foods.AsQueryable();

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
        }

        public async Task<Food> AddFoodAsync(Food food)
        {
            try
            {
                food.IsDeleted = false; // Ensure new food is not deleted

                await _context.Foods.AddAsync(food);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new food: {Name} (ID: {Id})", food.Name, food.Id);
                return food;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding food: {Name}", food.Name);
                throw;
            }
        }

        public async Task<Food> UpdateFoodAsync(Food food)
        {
            try
            {
                var existingFood = await _context.Foods
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

                _context.Foods.Update(existingFood);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated food: {Name} (ID: {Id})", food.Name, food.Id);
                return existingFood;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating food ID: {Id}", food.Id);
                throw;
            }
        }

        public async Task<bool> SoftDeleteFoodAsync(int id)
        {
            try
            {
                var food = await _context.Foods.FindAsync(id);
                if (food == null)
                    return false;

                food.IsDeleted = true;
                _context.Foods.Update(food);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft-deleted food ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting food ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> RestoreFoodAsync(int id)
        {
            try
            {
                var food = await _context.Foods
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsDeleted);

                if (food == null)
                    return false;

                food.IsDeleted = false;
                _context.Foods.Update(food);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Restored food ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring food ID: {Id}", id);
                return false;
            }
        }
    }
}
