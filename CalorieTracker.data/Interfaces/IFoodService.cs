using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.Interfaces
{
    public interface IFoodService
    {
        Task<Food?> GetFoodByIdAsync(int id);
        Task<List<Food>> SearchFoodsAsync(string searchTerm, bool includeDeleted = false);
        Task<Food> AddFoodAsync(Food food);
        Task<Food> UpdateFoodAsync(Food food);
        Task<bool> SoftDeleteFoodAsync(int id);
        Task<bool> RestoreFoodAsync(int id);
    }
}
