using System;
using System.Diagnostics;
using CalorieTracker.data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Resolve the LoadingPage from DI
            var loadingPage = _serviceProvider.GetRequiredService<Views.LoadingPage>();
            // Create window with LoadingPage
            var window = new Window(loadingPage);

            // Store window reference if needed
            // You can use this later if needed

            return window;
        }
    }
}