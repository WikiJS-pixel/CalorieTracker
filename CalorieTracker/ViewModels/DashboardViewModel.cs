using CalorieTracker.Data.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ICalorieTrackerService _trackerService;

        // Properties bound to the UI
        [ObservableProperty]
        private double _currentCalories;

        [ObservableProperty]
        private double _calorieGoal;

        [ObservableProperty]
        private double _remainingCalories;

        [ObservableProperty]
        private bool _isBusy;

        public DashboardViewModel(ICalorieTrackerService trackerService)
        {
            _trackerService = trackerService;
        }

        // Called when the user navigates to this page
        [RelayCommand]
        public async Task LoadDashboard()
        {
            IsBusy = true;
            try
            {
                var summaryWithGoals = await _trackerService.GetTodaySummaryWithGoalsAsync();
                var summary = summaryWithGoals.Summary;

                CurrentCalories = summary.TotalCalories;
                CalorieGoal = summary.TargetCalories;
                RemainingCalories = CalorieGoal - CurrentCalories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
