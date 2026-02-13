using System.Collections.ObjectModel;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalorieTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ICalorieTrackerService _trackerService;
        private readonly IErrorService _errorService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<DashboardViewModel> _logger;

        // Properties bound to the UI

        [ObservableProperty] private double _consumedCalories;
        [ObservableProperty] private double _calorieGoal;
        [ObservableProperty] private double _remainingCalories;
        [ObservableProperty] private double _calorieProgress; // 0.0 to 1.0

        [ObservableProperty] private double _consumedProtein;
        [ObservableProperty] private double _consumedCarbs;
        [ObservableProperty] private double _consumedFat;

        [ObservableProperty] private double _goalProtein;
        [ObservableProperty] private double _goalCarbs;
        [ObservableProperty] private double _goalFat;

        public ObservableCollection<MealSummary> Meals { get; } = [];

        public record MealSummary(MealType Type, string DisplayName, string Icon, double Calories);

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _hasLoggedMeals;

        public DashboardViewModel(
        ICalorieTrackerService trackerService,
        IErrorService errorService,
        IEventAggregator eventAggregator,
        ILogger<DashboardViewModel>? logger = null)
        {
            _trackerService = trackerService;
            _errorService = errorService;
            _eventAggregator = eventAggregator;
            _logger = logger ?? NullLogger<DashboardViewModel>.Instance;

            _logger.LogInformation("DashboardViewModel initialized");

            // Pre-fill meals with 0 (will be updated when real data exists)
            Meals.Add(new MealSummary(MealType.Breakfast, "Breakfast", "🥣", 0));
            Meals.Add(new MealSummary(MealType.Lunch, "Lunch", "🍴", 0));
            Meals.Add(new MealSummary(MealType.Dinner, "Dinner", "🍽️", 0));
            Meals.Add(new MealSummary(MealType.Snack, "Snack", "🍎", 0));

            _logger.LogInformation($"Meals count: {Meals.Count}");
        }

        // Called when the user navigates to this page
        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task LoadDashboard()
        {
            _logger.LogInformation("LoadDashboard called");

            if (IsBusy)
            {
                _logger.LogDebug("LoadDashboard aborted - already busy");
                return;
            }

            IsBusy = true;

            try
            {
                _logger.LogInformation("Fetching data from service...");
                // 1. Fetch data
                var todayData = await _trackerService.GetTodaySummaryWithGoalsAsync();

                // Guard clause: If service returns null (e.g. database empty), treat as empty
                if (todayData?.Summary == null || todayData?.Goals == null)
                {
                    _logger.LogWarning("Data is incomplete. Defaulting to empty state.");
                    ResetDashboardValues();
                    return;
                }

                // 2. Map consumed values (from today's MealEntries)
                ConsumedCalories = todayData.Summary.TotalCalories;
                ConsumedProtein = todayData.Summary.TotalProtein;
                ConsumedCarbs = todayData.Summary.TotalCarbs;
                ConsumedFat = todayData.Summary.TotalFat;

                // Goals
                CalorieGoal = todayData.Goals.TargetCalories;
                GoalProtein = todayData.Goals.TargetProtein;
                GoalCarbs = todayData.Goals.TargetCarbs;
                GoalFat = todayData.Goals.TargetFat;

                RemainingCalories = CalorieGoal - ConsumedCalories;
                CalorieProgress = CalorieGoal > 0 ? Math.Min(1.0, ConsumedCalories / CalorieGoal) : 0;

                // 3. Update the Meal List using the helper method
                try
                {
                    UpdateMealCollections(todayData.Summary.MealCalories);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating meal collections");
                }

                HasLoggedMeals = Meals.Any(m => m.Calories > 0);
                _logger.LogInformation("LoadDashboard completed successfully. HasLoggedMeals: {HasLoggedMeals}", HasLoggedMeals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in LoadDashboard: {Message}", ex.Message);
                await _errorService.ShowErrorAsync("Failed to load dashboard data");
            }
            finally
            {
                _logger.LogInformation("Setting IsBusy to false.");
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddToMeal(MealType mealType)
        {
            // Navigate to LogMealPage with pre-selected meal type
            await Shell.Current.GoToAsync($"{nameof(LogMealPage)}?SelectedMealType={(int)mealType}");
        }

        private void UpdateMealCollections(IDictionary<MealType, double>? mealCalories)
        {
            var data = mealCalories ?? new Dictionary<MealType, double>();

            // Map the dictionary values back to our ObservableCollection
            for (int i = 0; i < Meals.Count; i++)
            {
                var currentMeal = Meals[i];
                var newCalories = data.TryGetValue(currentMeal.Type, out var cal) ? cal : 0;

                // Update the record
                Meals[i] = currentMeal with { Calories = newCalories };
            }
        }

        private void ResetDashboardValues()
        {
            ConsumedCalories = 0;
            ConsumedProtein = 0;
            ConsumedCarbs = 0;
            ConsumedFat = 0;
            RemainingCalories = CalorieGoal;
            CalorieProgress = 0;
            HasLoggedMeals = false;

            // Reset meal calories to 0
            for (int i = 0; i < Meals.Count; i++)
            {
                Meals[i] = Meals[i] with { Calories = 0 };
            }
        }
    }
}
