using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class WeightService : IWeightService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<WeightService> _logger;
        private readonly IDatabaseLock _dbLock;

        public WeightService(
        IDbContextFactory dbContextFactory,
        IDatabaseLock dbLock,
        ILogger<WeightService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _dbLock = dbLock;
            _logger = logger;
        }

        public async Task<double?> GetCurrentWeightAsync()
        {
            await _dbLock.Semaphore.WaitAsync();
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
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<WeightLog>> GetWeightLogsAsync(int days = 30)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                // Create a new context for each operation
                using var context = _dbContextFactory.CreateContext();
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                return await context.WeightLogs
                    .Where(w => w.LogDate >= cutoffDate)
                    .OrderByDescending(w => w.LogDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight logs for {Days} days", days);
                return new List<WeightLog>();
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<WeightLog?> GetLatestWeightLogAsync()
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.WeightLogs
                    .OrderByDescending(w => w.LogDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest weight log");
                return null;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<WeightLog?> GetWeightLogByIdAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight log by ID: {Id}", id);
                return null;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<WeightLog> AddWeightLogAsync(WeightLog weightLog)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                // Set default log date if not provided
                if (weightLog.LogDate == default)
                    weightLog.LogDate = DateTime.UtcNow;

                await context.WeightLogs.AddAsync(weightLog);
                await context.SaveChangesAsync();

                _logger.LogInformation("Added weight log: {Weight}kg on {Date}",
                    weightLog.WeightKg, weightLog.LogDate);

                return weightLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding weight log");
                throw;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> UpdateWeightLogAsync(WeightLog weightLog)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var existingLog = await context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == weightLog.Id);

                if (existingLog == null)
                    return false;

                // Update properties
                existingLog.WeightKg = weightLog.WeightKg;
                existingLog.LogDate = weightLog.LogDate;
                existingLog.Notes = weightLog.Notes;

                context.WeightLogs.Update(existingLog);
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated weight log ID: {Id}", weightLog.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weight log ID: {Id}", weightLog.Id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<bool> DeleteWeightLogAsync(int id)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var weightLog = await context.WeightLogs
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (weightLog == null)
                    return false;

                context.WeightLogs.Remove(weightLog);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted weight log ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weight log ID: {Id}", id);
                return false;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<List<WeightLog>> GetWeightLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                return await context.WeightLogs
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
            finally 
            { 
                _dbLock.Semaphore.Release(); 
            }
        }

        public async Task<WeightLog?> GetWeightLogByDateAsync(DateTime date)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                // Get log for the specific date (ignoring time)
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                return await context.WeightLogs
                    .Where(w => w.LogDate >= startDate && w.LogDate <= endDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight log for date: {Date}", date);
                return null;
            }
            finally
            {
                _dbLock.Semaphore.Release();
            }
        }

        public async Task<double?> GetWeightChangeAsync(int days)
        {
            await _dbLock.Semaphore.WaitAsync();
            try
            {
                using var context = _dbContextFactory.CreateContext();
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                var logs = await context.WeightLogs
                    .Where(w => w.LogDate >= cutoffDate)
                    .OrderBy(w => w.LogDate)
                    .ToListAsync()
                    .ConfigureAwait(false);

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
            finally
            {
                _dbLock.Semaphore.Release(); 
            }
        }
    }
}
