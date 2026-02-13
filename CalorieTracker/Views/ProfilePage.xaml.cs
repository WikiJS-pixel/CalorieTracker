using CalorieTracker.ViewModels;

namespace CalorieTracker.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _profileViewModel;

    public ProfilePage(ProfileViewModel profileViewModel)
    {
        InitializeComponent();
        _profileViewModel = profileViewModel;
        BindingContext = _profileViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ProfileViewModel vm)
        {
            // Only initialize if not already loading
            if (!vm.IsLoading)
            {
                await vm.InitializeAsync();
            }
        }
    }
}