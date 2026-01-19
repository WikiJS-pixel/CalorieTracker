namespace CalorieTracker.Views;

public partial class LoadingPage : ContentPage
{
	public LoadingPage(ViewModels.LoadingViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}