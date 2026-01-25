using CalorieTracker.data.Services;
using CalorieTracker.Data.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.ViewModels
{
    public partial class LoadingViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;

        // UI State Properties
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoadingViewModel(IDatabaseService databaseService)  // Update constructor
        {
            _databaseService = databaseService;
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

                // Attempt Initialization (Database migration & Seeding)
                await _databaseService.InitializeAsync();

                // Navigate by setting the Window's Page to AppShell
                await SwitchToAppShellAsync();
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
    }
}
