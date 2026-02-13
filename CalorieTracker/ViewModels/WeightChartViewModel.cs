using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using static CalorieTracker.ViewModels.ViewModelErrorExtensions;

namespace CalorieTracker.ViewModels
{
    public partial class WeightChartViewModel : ObservableObject
    {
        private readonly IWeightService _weightService;
        private readonly IErrorService _errorService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<WeightChartViewModel> _logger;

        // bundle the repeated dependencies
        private readonly ErrorHandlingDependencies _errorDeps;

        [ObservableProperty]
        private PlotModel? _plotModel;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private List<WeightLog> _weightLogs = new();

        public WeightChartViewModel(
        IWeightService weightService,
        IErrorService errorService,
        IEventAggregator eventAggregator,
        ILogger<WeightChartViewModel> logger)
        {
            _weightService = weightService;
            _errorService = errorService;
            _eventAggregator = eventAggregator;
            _logger = logger;

            // Bundle once for reuse
            _errorDeps = new ErrorHandlingDependencies(_errorService, _eventAggregator, _logger);

            InitializeChartAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        public async Task InitializeChartAsync()
        {
            IsLoading = true;

            await this.ExecuteWithDualErrorHandlingAsync(
                async () =>
                {
                    var logs = await _weightService.GetWeightLogsAsync(90);
                    WeightLogs = logs.OrderBy(w => w.LogDate).ToList();
                    await UpdateChartAsync();
                },
                _errorDeps,
                nameof(InitializeChartAsync),
                options: new DualErrorHandlingOptions(ErrorTitle: "Failed to Load Chart")
            );

            IsLoading = false;
        }

        private async Task UpdateChartAsync()
        {
            if (!WeightLogs.Any())
            {
                await CreateEmptyChart();
                return;
            }

            // Build OxyPlot model in MAUI project only
            var model = new PlotModel
            {
                Title = "Weight History",
                TitleColor = OxyColors.Black,
                TitleFontSize = 18,
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.LightGray,
                IsLegendVisible = true
            };

            // Add legend configuration
            model.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendBorder = OxyColors.Gray,
                LegendBorderThickness = 1
            });

            // Date axis
            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Date",
                StringFormat = "MMM d",
                IntervalType = DateTimeIntervalType.Days,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromAColor(50, OxyColors.Gray),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromAColor(30, OxyColors.Gray),
                MinimumPadding = 0.05,  // Small padding on sides
                MaximumPadding = 0.05,
                IsZoomEnabled = true,
                IsPanEnabled = true
            };

            // Auto-span the axis with padding even for single points
            var minDate = WeightLogs.Min(w => w.LogDate);
            var maxDate = WeightLogs.Max(w => w.LogDate);
            var dateRange = maxDate - minDate;

            // If only one point or very few, extend the visible range for better UX
            if (dateRange.TotalDays < 30)
            {
                dateAxis.Minimum = DateTimeAxis.ToDouble(minDate.AddDays(-15));
                dateAxis.Maximum = DateTimeAxis.ToDouble(maxDate.AddDays(15));
            }

            model.Axes.Add(dateAxis);

            // Weight axis (left)
            var weightAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Weight (kg)",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromAColor(50, OxyColors.Gray),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromAColor(30, OxyColors.Gray),
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                AbsoluteMinimum = 0 // Prevent negative weights
            };

            // Add some padding above/below actual weights
            var minWeight = WeightLogs.Min(w => w.WeightKg);
            var maxWeight = WeightLogs.Max(w => w.WeightKg);
            var weightRange = maxWeight - minWeight;
            if (weightRange < 10)
            {
                weightAxis.Minimum = minWeight - 5;
                weightAxis.Maximum = maxWeight + 5;
            }

            model.Axes.Add(weightAxis);

            // Main weight line series
            var lineSeries = new LineSeries
            {
                Title = "Weight",
                Color = OxyColors.Blue,
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColors.Blue,
                MarkerStroke = OxyColors.DarkBlue,
                MarkerStrokeThickness = 2
            };

            foreach (var log in WeightLogs.OrderBy(w => w.LogDate))
            {
                lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(log.LogDate), log.WeightKg));
            }

            model.Series.Add(lineSeries);

            // Only add trend line if we have 2+ points
            if (WeightLogs.Count >= 2)
            {
                AddTrendLine(model);
            }

            PlotModel = model;
        }

        private void AddTrendLine(PlotModel model)
        {
            // Simple linear regression using your existing WeightLog data
            var points = WeightLogs
                .OrderBy(w => w.LogDate)
                .Select((w, i) => new { X = i, Y = w.WeightKg })
                .ToList();

            var n = points.Count;
            var sumX = points.Sum(p => p.X);
            var sumY = points.Sum(p => p.Y);
            var sumXY = points.Sum(p => p.X * p.Y);
            var sumX2 = points.Sum(p => p.X * p.X);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            var trendSeries = new LineSeries
            {
                Title = "Trend",
                Color = OxyColors.Red,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash
            };

            // Extend trend line slightly beyond data
            trendSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(WeightLogs.Min(w => w.LogDate).AddDays(-7)), intercept + slope * (-7)));
            trendSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(WeightLogs.Max(w => w.LogDate).AddDays(7)), intercept + slope * (n + 6)));

            model.Series.Add(trendSeries);
        }

        private Task CreateEmptyChart()
        {
            var model = new PlotModel
            {
                Title = "No Weight Logs Yet",
                TitleColor = OxyColors.Gray,
                TitleFontSize = 20,
                Background = OxyColors.Transparent
            };

            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Date" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Weight (kg)" });

            PlotModel = model;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task AddWeightAsync(double weight)
        {
            if (weight <= 0) return;

            await this.ExecuteWithDualErrorHandlingAsync(
                async () =>
                {
                    await _weightService.AddWeightLogAsync(new WeightLog
                    {
                        WeightKg = weight,
                        LogDate = DateTime.UtcNow,
                        Notes = "Added from chart"
                    });
                    await InitializeChartAsync();
                },
                _errorDeps,
                nameof(AddWeightAsync),
                options: new DualErrorHandlingOptions(
                    SuccessMessage: "Weight added successfully",
                    ErrorTitle: "Failed to Add Weight")
            );
        }
    }
}
