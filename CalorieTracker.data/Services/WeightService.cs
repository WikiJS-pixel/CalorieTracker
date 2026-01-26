using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class WeightService : IWeightService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WeightService> _logger;

        public WeightService(AppDbContext context, ILogger<WeightService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<double?> GetCurrentWeightAsync()
        {
            try
            {
                var latestLog = await GetLatestWeightLogAsync();
                return latestLog?.WeightKg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current weight");
                return null;
            }
        }

        public async Task<List<WeightLog>> GetWeightLogsAsync(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                return await _context.WeightLogs
                    .Where(w => w.LogDate >= cutoffDate)
                    .OrderByDescending(w => w.LogDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight logs for {Days} days", days);
                return new List<WeightLog>();
            }
        }

        public async Task<WeightLog?> GetLatestWeightLogAsync()
        {
            try
            {
                return await _context.WeightLogs
                    .OrderByDescending(w => w.LogDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest weight log");
                return null;
            }
        }

        public async Task<WeightLog?> GetWeightLogByIdAsync(int id)
        {
            try
            {
                return await _context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight log by ID: {Id}", id);
                return null;
            }
        }

        public async Task<WeightLog> AddWeightLogAsync(WeightLog weightLog)
        {
            try
            {
                // Set default log date if not provided
                if (weightLog.LogDate == default)
                    weightLog.LogDate = DateTime.UtcNow;

                await _context.WeightLogs.AddAsync(weightLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added weight log: {Weight}kg on {Date}",
                    weightLog.WeightKg, weightLog.LogDate);

                return weightLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding weight log");
                throw;
            }
        }

        public async Task<bool> UpdateWeightLogAsync(WeightLog weightLog)
        {
            try
            {
                var existingLog = await _context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == weightLog.Id);

                if (existingLog == null)
                    return false;

                // Update properties
                existingLog.WeightKg = weightLog.WeightKg;
                existingLog.LogDate = weightLog.LogDate;
                existingLog.Notes = weightLog.Notes;

                _context.WeightLogs.Update(existingLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated weight log ID: {Id}", weightLog.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weight log ID: {Id}", weightLog.Id);
                return false;
            }
        }

        public async Task<bool> DeleteWeightLogAsync(int id)
        {
            try
            {
                var weightLog = await _context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (weightLog == null)
                    return false;

                _context.WeightLogs.Remove(weightLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted weight log ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weight log ID: {Id}", id);
                return false;
            }
        }

        public async Task<List<WeightLog>> GetWeightLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.WeightLogs
                    .Where(w => w.LogDate >= startDate && w.LogDate <= endDate)
                    .OrderByDescending(w => w.LogDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight logs for range: {Start} to {End}",
                    startDate, endDate);
                return new List<WeightLog>();
            }
        }

        public async Task<WeightLog?> GetWeightLogByDateAsync(DateTime date)
        {
            try
            {
                // Get log for the specific date (ignoring time)
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                return await _context.WeightLogs
                    .Where(w => w.LogDate >= startDate && w.LogDate <= endDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight log for date: {Date}", date);
                return null;
            }
        }

        public async Task<double?> GetWeightChangeAsync(int days)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                var logs = await _context.WeightLogs
                    .Where(w => w.LogDate >= cutoffDate)
                    .OrderBy(w => w.LogDate)
                    .ToListAsync();

                if (logs.Count < 2)
                    return null;

                var oldestWeight = logs.First().WeightKg;
                var newestWeight = logs.Last().WeightKg;

                return newestWeight - oldestWeight;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating weight change for {Days} days", days);
                return null;
            }
        }
    }
}
