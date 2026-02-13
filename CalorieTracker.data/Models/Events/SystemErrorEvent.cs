namespace CalorieTracker.data.Models.Events
{
    public class SystemErrorEvent
    {
        public string Message { get; }
        public Exception? Exception { get; }
        public string Source { get; }
        public DateTime Timestamp { get; }
        public ErrorSeverity Severity { get; } // Info, Warning, Error, Critical

        public SystemErrorEvent(string message, Exception? exception,
            string source, ErrorSeverity severity = ErrorSeverity.Error)
        {
            Message = message;
            Exception = exception;
            Source = source;
            Timestamp = DateTime.UtcNow;
            Severity = severity;
        }
    }

    public enum ErrorSeverity
    {
        Info,      // For logging only
        Warning,   // Non-critical system issue
        Error,     // Recoverable error
        Critical   // Requires immediate attention
    }
}
