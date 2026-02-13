using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models.Events;
using CalorieTracker.data.Services;
using CalorieTracker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class LoadingViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IUserProfileService _userProfileService;
        private readonly IProfileRepository _profileRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<LoadingViewModel> _logger;

        // UI State Properties
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoadingViewModel(
            IDatabaseService databaseService,
            IUserProfileService userProfileService,
            IProfileRepository profileRepository,
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator,
            ILogger<LoadingViewModel> logger)
        {
            _databaseService = databaseService;
            _userProfileService = userProfileService;
            _profileRepository = profileRepository;
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        [RelayCommand]
        private async Task Retry()
        {
            await InitializeApp();
        }

        public async Task InitializeApp()
        {
            // 1. Reset State to "Loading"
            IsBusy = true;
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Add delay to see loading screen
                await Task.Delay(500);

                _logger.LogInformation("LoadingViewModel: Starting app initialization...");

                await _databaseService.InitializeAsync();
                _logger.LogInformation("LoadingViewModel: Database initialized");

                await _profileRepository.InitializeAsync();
                _logger.LogInformation("LoadingViewModel: ProfileRepository initialized");

                var profile = await _userProfileService.GetUserProfileAsync();
                _logger.LogInformation("LoadingViewModel: Profile loaded. HasCompletedWizard = {HasWizard}", profile.HasCompletedWizard);

#if DEBUG
                // Force wizard every time in debug builds (great for testing)
                profile.HasCompletedWizard = false;
                await _userProfileService.UpdateUserProfileAsync(profile);
#endif

                if (!profile.HasCompletedWizard)
                {
                    await SwitchToWizardAsync();
                }
                else
                {
                    await SwitchToAppShellAsync();
                }
            }
            catch (Exception ex)
            {
                // 4. Failure - Show Error UI
                IsBusy = false;
                IsError = true;

                // Friendly error message for users, specific log for you
                ErrorMessage = $"Unable to setup database.\nDetails: {ex.Message}";

                _logger.LogError(ex, "LoadingViewModel: Critical error during app initialization");

                await this.ExecuteWithSystemErrorHandlingAsync(
                async () => throw ex, // Re-throw to trigger error handling
                _eventAggregator,
                _logger,
                nameof(InitializeApp),
                ErrorSeverity.Critical // Critical - app can't start
            );

            }
        }

        private async Task SwitchToAppShellAsync()
        {
            var appShell = _serviceProvider.GetRequiredService<AppShell>(); // Resolve from DI (singleton)
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Application.Current?.Windows is { Count: > 0 })
                {
                    Application.Current.Windows[0].Page = appShell;
                }
                else
                {
                    throw new InvalidOperationException("No application window available");
                }
            });
        }

        private async Task SwitchToWizardAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Application.Current?.Windows is { Count: > 0 })
                {
                    var wizardPage = _serviceProvider.GetRequiredService<WizardPage>();
                    Application.Current.Windows[0].Page = wizardPage;
                }
                else
                {
                    throw new InvalidOperationException("No application window available");
                }
            });
        }
    }
}
