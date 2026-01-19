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
            return new Window(loadingPage);
        }
    }
}