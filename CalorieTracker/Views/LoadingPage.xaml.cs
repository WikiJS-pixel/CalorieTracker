using CalorieTracker.ViewModels;

namespace CalorieTracker.Views;

public partial class LoadingPage : ContentPage
{
	public LoadingPage(ViewModels.LoadingViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

    // This runs when the page becomes visible
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LoadingViewModel viewModel)
        {
            await viewModel.InitializeApp();
        }
    }
}