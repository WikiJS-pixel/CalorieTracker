using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models.Events;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Extensions.Logging;
// Alias to resolve the ambiguous 'Font' reference
using Font = Microsoft.Maui.Font;

namespace CalorieTracker.Services
{
    public class MauiErrorService : IErrorService
    {
        private readonly ILogger<MauiErrorService> _logger;
        private readonly IEventAggregator _eventAggregator;

        public MauiErrorService(ILogger<MauiErrorService> logger, IEventAggregator eventAggregator)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;

            // Subscribe to system errors to log them
            _eventAggregator.Subscribe<SystemErrorEvent>(OnSystemError);

            // Optionally subscribe to AppErrorEvent if others are publishing it
            _eventAggregator.Subscribe<AppErrorEvent>(OnAppError);
        }

        public async Task ShowErrorAsync(string message, string? title = null)
        {
            var fullMessage = $"{title ?? "Error"}: {message}";
            await ShowSnackbarAsync(fullMessage, Colors.Red, Colors.White, TimeSpan.FromSeconds(4));
            _logger.LogError("User error shown: {Message}", message);

            // Optionally publish an event
            await _eventAggregator.PublishAsync(new AppErrorEvent(fullMessage, null, nameof(MauiErrorService)));
        }

        public async Task ShowWarningAsync(string message, string? title = null)
        {
            var fullMessage = $"{title ?? "Warning"}: {message}";
            await ShowSnackbarAsync(fullMessage, Colors.Orange, Colors.Black, TimeSpan.FromSeconds(4));
            _logger.LogWarning("User warning shown: {Message}", message);
        }

        public async Task ShowInfoAsync(string message, string? title = null)
        {
            var fullMessage = $"{title ?? "Info"}: {message}";
            await ShowSnackbarAsync(fullMessage, Colors.Blue, Colors.White, TimeSpan.FromSeconds(3));
            _logger.LogInformation("User info shown: {Message}", message);
        }

        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            var fullMessage = $"{title ?? "Success"}: {message}";
            await ShowSnackbarAsync(fullMessage, Colors.Green, Colors.White, TimeSpan.FromSeconds(2));
            _logger.LogInformation("User success shown: {Message}", message);
        }

        private async Task ShowSnackbarAsync(string message, Color bgColor, Color textColor, TimeSpan duration)
        {
            try
            {
                // Ensure we are on the main thread for UI operations
                if (!MainThread.IsMainThread)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await CreateAndShowSnackbar(message, bgColor, textColor, duration);
                    });
                }
                else
                {
                    await CreateAndShowSnackbar(message, bgColor, textColor, duration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show snackbar: {Message}", message);

                // Fallback to console output
                System.Diagnostics.Debug.WriteLine($"[SNACKBAR FAILED] {message}");
            }
        }

        private async Task CreateAndShowSnackbar(string message, Color bgColor, Color textColor, TimeSpan duration)
        {
            // Configure Snackbar options with full customization
            var options = new SnackbarOptions
            {
                BackgroundColor = bgColor,
                TextColor = textColor,
                ActionButtonTextColor = Colors.Yellow, // Action button color
                CornerRadius = new CornerRadius(12),   // Rounded corners
                Font = Font.SystemFontOfSize(14),      // Font size
                CharacterSpacing = 0.5                 // Letter spacing
            };

            // Create Snackbar with dismiss action
            var snackbar = Snackbar.Make(
                message: message,
                action: () =>
                {
                    // Action when user taps the action button
                    _logger.LogDebug("Snackbar action tapped for: {Message}", message);
                },
                actionButtonText: "Dismiss",           // Button text
                duration: duration,                    // How long it stays visible
                visualOptions: options                 // Custom styling
            );

            await snackbar.Show();
        }

        private void OnSystemError(SystemErrorEvent error)
        {
            // Log all system errors
            _logger.Log(error.Severity switch
            {
                ErrorSeverity.Info => LogLevel.Information,
                ErrorSeverity.Warning => LogLevel.Warning,
                ErrorSeverity.Error => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Error
            }, error.Exception, "System error from {Source}: {Message}",
               error.Source, error.Message);

            // Show only critical errors to users
            if (error.Severity == ErrorSeverity.Critical)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ShowErrorAsync(error.Message, "Critical System Error");
                });
            }
        }

        private void OnAppError(AppErrorEvent error)
        {
            // Forward AppErrorEvent to UI (if you want to use AppErrorEvent pattern)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ShowErrorAsync(error.Message, error.Source);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string message, string? title = null)
        {
            if (!MainThread.IsMainThread)
            {
                return await MainThread.InvokeOnMainThreadAsync(() =>
                    ShowConfirmationAsync(message, title));
            }

            return await Application.Current.MainPage.DisplayAlert(
                title ?? "Confirm",
                message,
                "Yes",
                "No"
            );
        }
    }
}
