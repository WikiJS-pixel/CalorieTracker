namespace CalorieTracker.data.Models.Events
{
    public class SaveCompletedEvent
    {
        public bool Success { get; }
        public Exception? Error { get; }
        public DateTime Timestamp { get; }

        public SaveCompletedEvent(bool success, Exception? error = null)
        {
            Success = success;
            Error = error;
            Timestamp = DateTime.UtcNow;
        }
    }
}
