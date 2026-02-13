using CalorieTracker.ViewModels;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ILogger<DashboardPage> _logger;

    public DashboardPage(DashboardViewModel viewModel, ILogger<DashboardPage> logger)
    {
        _logger = logger;

        _logger.LogInformation("DashboardPage constructor called");
        InitializeComponent();
        BindingContext = viewModel;

        _logger.LogInformation($"After InitializeComponent, BindingContext is null: {BindingContext == null}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogInformation($"DashboardPage.OnAppearing() called. BindingContext is null: {BindingContext == null}");

        if (BindingContext is DashboardViewModel viewModel)
        {
            _logger.LogInformation("Calling LoadDashboardCommand...");

            // Use ExecuteAsync instead of Execute
            if (viewModel.LoadDashboardCommand.CanExecute(null))
            {
                await viewModel.LoadDashboardCommand.ExecuteAsync(null);
            }
            else
            {
                _logger.LogWarning("LoadDashboardCommand cannot execute currently");
            }
        }
        else
        {
            _logger.LogError("ERROR: BindingContext is not DashboardViewModel! Type: {Type}",
                BindingContext?.GetType().Name ?? "null");
        }
    }
}