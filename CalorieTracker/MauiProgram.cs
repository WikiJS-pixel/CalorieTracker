using CalorieTracker.data;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Services;
using CalorieTracker.Services;
using CalorieTracker.ViewModels;
using CalorieTracker.Views;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OxyPlot.Maui.Skia;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace CalorieTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseOxyPlotSkia()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true); 
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 1. Establish the platform-specific database path
            // On Mobile: /data/user/0/com.company.app/files/calories.db
            // On Windows: C:\Users\Name\AppData\Local\Packages\...
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "calorietracker.db");
            System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}");

            // 2. Register AppDbContext with the dynamic path
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}")
                .EnableSensitiveDataLogging() 
                .LogTo(message => System.Diagnostics.Debug.WriteLine(message),
                  LogLevel.Information)); 

            // Register the Generic DataService (Open Generic)
            // This allows you to inject IDataService<AnyEntity> anywhere
            builder.Services.AddScoped(typeof(IDataService<>), typeof(DataService<>));

            // Add the runtime factory
            builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();

            // REGISTER CORE ARCHITECTURE SERVICES 
            builder.Services.AddSingleton<IEventAggregator, EventAggregator>();
            builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
            builder.Services.AddSingleton<IAppStateService, AppStateService>();
            builder.Services.AddSingleton<IProfileRepository, ProfileRepository>();
            builder.Services.AddSingleton<ProfileRepository>();
            builder.Services.AddSingleton<IDatabaseLock, DatabaseLock>();

            // Register Data Services (from your library)
            builder.Services.AddScoped<IDatabaseService, DatabaseService>();
            builder.Services.AddScoped<ICalorieTrackerService, CalorieTrackerService>();
            builder.Services.AddScoped<IFoodService, FoodService>();
            builder.Services.AddScoped<IGoalCalculationService, GoalCalculationService>();
            builder.Services.AddScoped<IMealEntryService, MealEntryService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IWeightService, WeightService>();

            // Register MAUI-Specific Services
            builder.Services.AddSingleton<IFoodDataSeeder, MauiFoodDataSeeder>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IErrorService, MauiErrorService>();
            builder.Services.AddSingleton<AppShell>();

            // Register ViewModels
            builder.Services.AddTransient<WeightChartViewModel>(provider =>
                new WeightChartViewModel(
                    provider.GetRequiredService<IWeightService>(),
                    provider.GetRequiredService<IErrorService>(),
                    provider.GetRequiredService<IEventAggregator>(),
                    provider.GetRequiredService<ILogger<WeightChartViewModel>>()));

            
            builder.Services.AddTransient<LoadingViewModel>(provider =>
                new LoadingViewModel(
                    provider.GetRequiredService<IDatabaseService>(),
                    provider.GetRequiredService<IUserProfileService>(),
                    provider.GetRequiredService<IProfileRepository>(),
                    provider.GetRequiredService<IServiceProvider>(),
                    provider.GetRequiredService<IEventAggregator>(),
                    provider.GetRequiredService<ILogger<LoadingViewModel>>()));

            builder.Services.AddTransient<WizardViewModel>(provider =>
                new WizardViewModel(
                    provider.GetRequiredService<IUserProfileService>(),
                    provider.GetRequiredService<IWeightService>(),
                    provider.GetRequiredService<IGoalCalculationService>(),
                    provider.GetRequiredService<IEventAggregator>(),
                    provider.GetRequiredService<IErrorService>(),
                    provider.GetRequiredService<IAppStateService>(),
                    provider.GetRequiredService<ILogger<WizardViewModel>>()));

            builder.Services.AddTransient<DashboardViewModel>(provider => 
                new DashboardViewModel(
                    provider.GetRequiredService<ICalorieTrackerService>(),
                    provider.GetRequiredService<IErrorService>(),
                    provider.GetRequiredService<IEventAggregator>(),
                    provider.GetRequiredService<ILogger<DashboardViewModel>>()));

            builder.Services.AddSingleton<ProfileEditorViewModel>();
            builder.Services.AddTransient<WeightTrackerViewModel>();
            builder.Services.AddTransient<GoalsViewModel>();

            // as a container for the other ViewModels
            builder.Services.AddTransient<ProfileViewModel>(provider =>
                new ProfileViewModel(
                    provider.GetRequiredService<ProfileEditorViewModel>(),
                    provider.GetRequiredService<WeightTrackerViewModel>(),
                    provider.GetRequiredService<WeightChartViewModel>(),
                    provider.GetRequiredService<GoalsViewModel>(),
                    provider.GetRequiredService<IErrorService>(),
                    provider.GetRequiredService<IProfileRepository>(),
                    provider.GetRequiredService<ILogger<ProfileViewModel>>()));

            // Register views
            builder.Services.AddTransient<Views.LoadingPage>();
            builder.Services.AddTransient<Views.DashboardPage>();
            builder.Services.AddTransient<Views.LogMealPage>();
            builder.Services.AddTransient<Views.HistoryPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<Views.WizardPage>();
            builder.Services.AddTransient<Views.Step0PersonalInfo>();
            builder.Services.AddTransient<Views.Step1PhysicalStats>();
            builder.Services.AddTransient<Views.Step2ActivityGoals>();
            builder.Services.AddTransient<Views.Step3DietaryPrefs>();
            builder.Services.AddTransient<Views.Step4Preview>();
            

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
