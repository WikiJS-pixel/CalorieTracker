using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class WeightTrackerViewModel : ObservableObject
    {
        private readonly IProfileRepository _repository;
        private readonly IGoalCalculationService _goalCalculationService;
        private readonly IErrorService _errorService;
        private readonly ILogger<WeightTrackerViewModel> _logger;
        private readonly IEventAggregator _eventAggregator;

        // Current weight display
        [ObservableProperty]
        private double? _currentWeight;

        [ObservableProperty]
        private DateTime? _lastWeightLogDate;

        // Chart data - you can keep your existing WeightChartViewModel
        // or integrate its functionality here
        public List<WeightLog> WeightLogs => _repository.WeightLogs;

        public WeightTrackerViewModel(
            IProfileRepository repository,
            IGoalCalculationService goalCalculationService,
            IErrorService errorService,
            ILogger<WeightTrackerViewModel> logger,
            IEventAggregator eventAggregator)
        {
            _repository = repository;
            _goalCalculationService = goalCalculationService;
            _errorService = errorService;
            _logger = logger;
            _eventAggregator = eventAggregator;

            SubscribeToEvents();
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            UpdateCurrentWeight();
        }

        [RelayCommand]
        public async Task LogWeightAsync()
        {
            try
            {
                string result = await Application.Current!.Windows[0].Page!.DisplayPromptAsync(
                    "Log Weight",
                    "Enter current weight (kg):",
                    keyboard: Keyboard.Numeric);

                if (double.TryParse(result, out double weight))
                {
                    var weightLog = new WeightLog
                    {
                        WeightKg = weight,
                        LogDate = DateTime.UtcNow
                    };

                    await _repository.AddWeightLogAsync(weightLog);

                    // Ask about recalculating goals
                    bool recalculate = await _errorService.ShowConfirmationAsync(
                        "Weight Changed",
                        "Recalculate your calorie goals based on this new weight?");

                    if (recalculate)
                    {
                        await RecalculateGoalsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log weight");
                await _errorService.ShowErrorAsync("Failed to log weight", "Error");
            }
        }

        [RelayCommand]
        public async Task RecalculateGoalsAsync()
        {
            try
            {
                // Invalidate cache to force recalculation
                _goalCalculationService.InvalidateCache();

                // Trigger goal recalculation
                await _goalCalculationService.CalculateDailyGoalsAsync();

                await _errorService.ShowSuccessAsync("Goals recalculated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate goals");
                await _errorService.ShowErrorAsync("Failed to recalculate goals", "Error");
            }
        }

        private void UpdateCurrentWeight()
        {
            var latestLog = WeightLogs.OrderByDescending(w => w.LogDate).FirstOrDefault();
            CurrentWeight = latestLog?.WeightKg;
            LastWeightLogDate = latestLog?.LogDate;

            OnPropertyChanged(nameof(WeightLogs));
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.Subscribe<WeightLogsChangedEvent>(OnWeightLogsChanged);
        }

        private void OnWeightLogsChanged(WeightLogsChangedEvent e)
        {
            UpdateCurrentWeight();
        }
    }
}
