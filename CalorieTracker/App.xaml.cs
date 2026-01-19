using CalorieTracker.data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker
{
    public partial class App : Application
    {
        private readonly IDatabaseService _databaseService;
        public App(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Handle initialization when the window is created
            window.Created += async (s, e) =>
            {
                try
                {
                    await _databaseService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    // If DB fails, you can redirect to an error page 
                    // or show a platform-specific alert
                    Debug.WriteLine($"Startup Error: {ex.Message}");
                }
            };

            return window;
        }
    }
}