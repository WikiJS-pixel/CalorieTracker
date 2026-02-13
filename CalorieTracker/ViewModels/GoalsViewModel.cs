using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class GoalsViewModel : ObservableObject
    {
        private readonly IGoalCalculationService _goalCalculationService;
        private readonly ILogger<GoalsViewModel> _logger;
        private readonly IEventAggregator _eventAggregator;

        [ObservableProperty]
        private DailyGoals? _dailyGoals;

        public GoalsViewModel(
            IGoalCalculationService goalCalculationService,
            ILogger<GoalsViewModel> logger,
            IEventAggregator eventAggregator)
        {
            _goalCalculationService = goalCalculationService;
            _logger = logger;
            _eventAggregator = eventAggregator;

            SubscribeToEvents();
        }

        [RelayCommand]
        public async Task LoadGoalsAsync()
        {
            try
            {
                DailyGoals = await _goalCalculationService.CalculateDailyGoalsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load goals");
            }
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.Subscribe<GoalsRecalculatedEvent>(OnGoalsRecalculated);
            _eventAggregator.Subscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            _eventAggregator.Subscribe<WeightLogsChangedEvent>(OnWeightLogsChanged);
        }

        private void OnGoalsRecalculated(GoalsRecalculatedEvent e)
        {
            DailyGoals = e.Goals;
        }

        private void OnProfileUpdated(ProfileUpdatedEvent e)
        {
            // Profile changed, goals might need recalculation
            _ = LoadGoalsAsync();
        }

        private void OnWeightLogsChanged(WeightLogsChangedEvent e)
        {
            // Weight changed, goals might need recalculation
            _ = LoadGoalsAsync();
        }
    }
}
