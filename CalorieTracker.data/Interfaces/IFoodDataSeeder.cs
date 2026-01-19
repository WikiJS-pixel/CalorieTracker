using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.Interfaces
{
    public interface IFoodDataSeeder
    {
        Task<List<Food>> GetSeedFoodsAsync(CancellationToken ct = default);
    }
}
