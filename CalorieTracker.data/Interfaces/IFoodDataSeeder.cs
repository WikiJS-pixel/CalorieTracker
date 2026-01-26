using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IFoodDataSeeder
    {
        Task<List<Food>> GetSeedFoodsAsync(CancellationToken ct = default);
    }
}
