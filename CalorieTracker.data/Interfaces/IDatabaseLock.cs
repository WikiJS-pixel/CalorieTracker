namespace CalorieTracker.data.Interfaces
{
    public interface IDatabaseLock
    {
        SemaphoreSlim Semaphore { get; }
    }
}
