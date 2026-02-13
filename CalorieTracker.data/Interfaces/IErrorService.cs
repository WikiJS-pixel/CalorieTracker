namespace CalorieTracker.data.Interfaces
{
    public interface IErrorService
    {
        Task ShowErrorAsync(string message, string? title = null);
        Task ShowWarningAsync(string message, string? title = null);
        Task ShowInfoAsync(string message, string? title = null);
        Task ShowSuccessAsync(string message, string? title = null);
        Task<bool> ShowConfirmationAsync(string message, string? title = null);
    }
}
