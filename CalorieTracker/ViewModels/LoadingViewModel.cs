using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Services;
using CalorieTracker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.ViewModels
{
    public partial class LoadingViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IUserProfileService _userProfileService;
        private readonly IServiceProvider _serviceProvider;

        // UI State Properties
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoadingViewModel(IDatabaseService databaseService,
            IUserProfileService userProfileService,
            IServiceProvider serviceProvider)
        {
            _databaseService = databaseService;
            _userProfileService = userProfileService;
            _serviceProvider = serviceProvider;
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
                await Task.Delay(2000); // 2 seconds

                var profile = await _userProfileService.GetUserProfileAsync();
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
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }

        private async Task SwitchToAppShellAsync()
        {
            try
            {
                // Switch from LoadingPage to AppShell
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        // Replace the current page (LoadingPage) with AppShell
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                    else
                    {
                        throw new InvalidOperationException("No application window available");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SwitchToAppShellAsync Error: {ex}");
                throw;
            }
        }

        private async Task SwitchToWizardAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    var wizardPage = _serviceProvider.GetRequiredService<WizardPage>();
                    Application.Current.Windows[0].Page = wizardPage;
                }
            });
        }
    }
}
