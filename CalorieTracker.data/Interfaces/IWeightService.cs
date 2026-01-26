using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IWeightService
    {
        Task<double?> GetCurrentWeightAsync();
        Task<List<WeightLog>> GetWeightLogsAsync(int days = 30);
        Task<WeightLog?> GetLatestWeightLogAsync();
        Task<WeightLog?> GetWeightLogByIdAsync(int id);
        Task<WeightLog> AddWeightLogAsync(WeightLog weightLog);
        Task<bool> UpdateWeightLogAsync(WeightLog weightLog);
        Task<bool> DeleteWeightLogAsync(int id);
        Task<List<WeightLog>> GetWeightLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<WeightLog?> GetWeightLogByDateAsync(DateTime date);
        Task<double?> GetWeightChangeAsync(int days);
    }
}
