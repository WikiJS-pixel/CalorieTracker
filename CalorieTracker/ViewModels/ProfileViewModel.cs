using CalorieTracker.data.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IErrorService _errorService;
        private readonly ILogger<ProfileViewModel> _logger;
        private readonly IProfileRepository _repository;

        [ObservableProperty] private ProfileEditorViewModel _editorViewModel;
        [ObservableProperty] private WeightTrackerViewModel _weightViewModel;
        [ObservableProperty] private WeightChartViewModel _chartViewModel;
        [ObservableProperty]private GoalsViewModel _goalsViewModel;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _loadingMessage = "Loading...";

        public ProfileViewModel(
            ProfileEditorViewModel editorViewModel,
            WeightTrackerViewModel weightViewModel,
            WeightChartViewModel chartViewModel,
            GoalsViewModel goalsViewModel,
            IErrorService errorService,
            IProfileRepository repository,
            ILogger<ProfileViewModel> logger)
        {
            EditorViewModel = editorViewModel;
            WeightViewModel = weightViewModel;
            ChartViewModel = chartViewModel;
            GoalsViewModel = goalsViewModel;
            _errorService = errorService;
            _repository = repository;
            _logger = logger;
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            if (IsLoading) return;

            IsLoading = true;

            try
            {
                _logger.LogInformation("Starting sequential profile initialization");

                // Step 0: Initialize repository first (only once)
                if (!_repository.IsInitialized) 
                {
                    await _repository.InitializeAsync();
                    await Task.Delay(200); 
                }

                // Step 1: Initialize Editor ViewModel first
                LoadingMessage = "Loading profile data...";
                await EditorViewModel.InitializeAsync();
                await Task.Delay(100); // Small delay to prevent context overlap

                // Step 2: Initialize Weight ViewModel
                LoadingMessage = "Loading weight data...";
                await WeightViewModel.InitializeAsync();
                await Task.Delay(100);

                // Step 3: Initialize Chart ViewModel
                LoadingMessage = "Loading charts...";
                await ChartViewModel.InitializeChartAsync();

                // Step 4: Initialize Goals ViewModel (if it exists)
                if (GoalsViewModel != null)
                {
                    LoadingMessage = "Calculating goals...";
                    await GoalsViewModel.LoadGoalsAsync();
                }

                _logger.LogInformation("Profile initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize profile");
                await _errorService.ShowErrorAsync("Failed to load profile data");
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = "Loading...";
            }
        }
    }
}
