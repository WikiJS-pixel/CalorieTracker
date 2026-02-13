using CalorieTracker.data.Interfaces;

namespace CalorieTracker.data.Services
{
    public class DatabaseLock : IDatabaseLock
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    }
}
