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

            // Register the Generic DataService (Open Generic)
            // This allows you to inject IDataService<AnyEntity> anywhere
            builder.Services.AddScoped(typeof(IDataService<>), typeof(DataService<>));

            // 2. Register AppDbContext with the dynamic path
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // 3. Register Data Services (from your library)
            builder.Services.AddScoped<IDatabaseService, DatabaseService>();
            builder.Services.AddScoped<ICalorieTrackerService, CalorieTrackerService>();
            builder.Services.AddScoped<IFoodService, FoodService>();
            builder.Services.AddScoped<IGoalCalculationService, GoalCalculationService>();
            builder.Services.AddScoped<IMealEntryService, MealEntryService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();

            // 4. Register MAUI-Specific Services (Moved from Data)
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IFoodDataSeeder, MauiFoodDataSeeder>();

            // 5. Register ViewModels and Views (MAUI Project)
            builder.Services.AddTransient<Views.LoadingPage>();
            builder.Services.AddTransient<ViewModels.LoadingViewModel>();
            builder.Services.AddTransient<Views.DashboardPage>();
            builder.Services.AddTransient<ViewModels.DashboardViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
