namespace CalorieTracker.data.Models.Events
{
    public class AppErrorEvent
    {
        public string Message { get; }
        public Exception? Exception { get; }
        public string? Source { get; }

        public AppErrorEvent(string message, Exception? exception = null, string? source = null)
        {
            Message = message;
            Exception = exception;
            Source = source;
        }
    }
}
