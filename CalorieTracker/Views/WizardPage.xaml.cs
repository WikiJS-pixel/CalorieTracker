using CalorieTracker.ViewModels;

namespace CalorieTracker.Views;

public partial class WizardPage : ContentPage
{
    public WizardPage(WizardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}