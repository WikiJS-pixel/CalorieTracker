using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Services;
using CalorieTracker.Data;
using CalorieTracker.Data.Interfaces;
using CalorieTracker.Services;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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

            // 3. Register Data Services (from your library)
            builder.Services.AddScoped<IDatabaseService, DatabaseService>();
            builder.Services.AddScoped<ICalorieTrackerService, CalorieTrackerService>();
            builder.Services.AddScoped<IFoodService, FoodService>();
            builder.Services.AddScoped<IGoalCalculationService, GoalCalculationService>();
            builder.Services.AddScoped<IMealEntryService, MealEntryService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IWeightService, WeightService>();

            // 4. Register MAUI-Specific Services
            builder.Services.AddSingleton<IFoodDataSeeder, MauiFoodDataSeeder>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<AppShell>();

            // 5. Register ViewModels and Views (MAUI Project)
            builder.Services.AddTransient<Views.LoadingPage>();
            builder.Services.AddTransient<ViewModels.LoadingViewModel>();
            builder.Services.AddTransient<Views.DashboardPage>();
            builder.Services.AddTransient<ViewModels.DashboardViewModel>();
            builder.Services.AddTransient<Views.LogMealPage>();
            builder.Services.AddTransient<Views.HistoryPage>();
            builder.Services.AddTransient<Views.ProfilePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
