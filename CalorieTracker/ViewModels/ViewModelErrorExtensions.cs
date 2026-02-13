using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models.Events;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public static class ViewModelErrorExtensions
    {
        // USER-FACING ERRORS (IErrorService)
        public static async Task ExecuteWithUserErrorHandlingAsync(
            this object viewModel,
            Func<Task> operation,
            IErrorService errorService,
            string successMessage = "",
            string errorTitle = "Error",
            string? successTitle = null)
        {
            try
            {
                await operation();

                if (!string.IsNullOrEmpty(successMessage))
                {
                    await errorService.ShowSuccessAsync(successMessage, successTitle);
                }
            }
            catch (Exception ex)
            {
                await errorService.ShowErrorAsync(ex.Message, errorTitle);
            }
        }

        public static async Task<T?> ExecuteWithUserErrorHandlingAsync<T>(
            this object viewModel,
            Func<Task<T>> operation,
            IErrorService errorService,
            string errorTitle = "Error",
            T? defaultValue = default)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                await errorService.ShowErrorAsync(ex.Message, errorTitle);
                return defaultValue;
            }
        }

        // SYSTEM/BACKGROUND ERRORS (EventAggregator + ILogger)
        public static async Task ExecuteWithSystemErrorHandlingAsync(
            this object viewModel,
            Func<Task> operation,
            IEventAggregator eventAggregator,
            ILogger logger,
            string source,
            ErrorSeverity severity = ErrorSeverity.Error)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "System error in {Source}: {Message}", source, ex.Message);

                await eventAggregator.PublishAsync(new SystemErrorEvent(
                    ex.Message,
                    ex,
                    source,
                    severity
                ));
            }
        }

        // DUAL STRATEGY (Both user-facing AND system errors)
        public static async Task<T?> ExecuteWithDualErrorHandlingAsync<T>(
            this object viewModel,
            Func<Task<T>> operation,
            ErrorHandlingDependencies dependencies,
            string source,
            DualErrorHandlingOptions? options = null,
            T? defaultValue = default)
        {
            options ??= new DualErrorHandlingOptions();

            try
            {
                var result = await operation();

                if (!string.IsNullOrEmpty(options.SuccessMessage))
                {
                    await dependencies.ErrorService.ShowSuccessAsync(options.SuccessMessage, "Success");
                }

                return result;
            }
            catch (Exception ex)
            {
                // Show to user
                await dependencies.ErrorService.ShowErrorAsync(ex.Message, options.ErrorTitle);

                // Log to system
                dependencies.Logger.LogError(ex, "Error in {Source}: {Message}", source, ex.Message);
                await dependencies.EventAggregator.PublishAsync(new SystemErrorEvent(
                    ex.Message,
                    ex,
                    source,
                    options.SystemErrorSeverity
                ));

                return defaultValue;
            }
        }

        public static async Task ExecuteWithDualErrorHandlingAsync(
            this object viewModel,
            Func<Task> operation,
            ErrorHandlingDependencies dependencies,
            string source,
            DualErrorHandlingOptions? options = null)
        {
            options ??= new DualErrorHandlingOptions();

            try
            {
                await operation();

                if (!string.IsNullOrEmpty(options.SuccessMessage))
                {
                    await dependencies.ErrorService.ShowSuccessAsync(options.SuccessMessage, "Success");
                }
            }
            catch (Exception ex)
            {
                await dependencies.ErrorService.ShowErrorAsync(ex.Message, options.ErrorTitle);

                dependencies.Logger.LogError(ex, "Error in {Source}: {Message}", source, ex.Message);
                await dependencies.EventAggregator.PublishAsync(new SystemErrorEvent(
                    ex.Message,
                    ex,
                    source,
                    options.SystemErrorSeverity
                ));
            }
        }

        public record ErrorHandlingDependencies(
            IErrorService ErrorService,
            IEventAggregator EventAggregator,
            ILogger Logger);

        public record DualErrorHandlingOptions(
            string ErrorTitle = "Error",
            string? SuccessMessage = null,
            string SuccessTitle = "Success",
            ErrorSeverity SystemErrorSeverity = ErrorSeverity.Error);
    }
}
