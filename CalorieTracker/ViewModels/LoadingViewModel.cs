using CalorieTracker.data.Services;
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

        public LoadingViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            InitializeApp();
        }

        [RelayCommand]
        private async Task Retry()
        {
            await InitializeApp();
        }

        private async Task InitializeApp()
        {
            // 1. Reset State to "Loading"
            IsBusy = true;
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                // 2. Attempt Initialization (Database migration & Seeding)
                // This mimics a "transaction", if it fails, we catch it below
                await _databaseService.InitializeAsync();

                // 3. Success! Swap the Main Page
                if (Application.Current != null)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            }
            catch (Exception ex)
            {
                // 4. Failure - Show Error UI
                IsBusy = false;
                IsError = true;

                // Friendly error message for users, specific log for you
                ErrorMessage = $"Unable to setup database.\nDetails: {ex.Message}";
            }
        }
    }
}
