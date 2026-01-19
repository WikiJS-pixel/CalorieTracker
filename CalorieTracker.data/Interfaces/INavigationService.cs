using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.Interfaces
{
    public interface INavigationService
    {
        // Navigation methods
        Task NavigateToDashboardAsync();
        Task NavigateToLogMealAsync(Food? preselectedFood = null);
        Task NavigateToLogMealAsync(int foodId);
        Task NavigateToProfileAsync();
        Task NavigateToHistoryAsync();
        Task NavigateToHistoryDetailAsync(DateTime date);
        Task NavigateToFoodSearchAsync();
        Task NavigateToWeightLogAsync();
        Task GoBackAsync();

        // State management
        T GetNavigationParameter<T>(string key);
        void SetNavigationParameter(string key, object value);
        void ClearNavigationParameters();
    }
}
