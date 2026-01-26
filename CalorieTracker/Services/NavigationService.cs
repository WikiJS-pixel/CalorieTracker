using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;

namespace CalorieTracker.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<string, object> _parameters = [];

        public async Task NavigateToDashboardAsync()
        {
            await Shell.Current.GoToAsync("//Dashboard");
        }

        public async Task NavigateToLogMealAsync(Food? preselectedFood = null)
        {
            if (preselectedFood != null)
            {
                SetNavigationParameter("PreselectedFood", preselectedFood);
            }
            await Shell.Current.GoToAsync("//LogMeal");
        }

        public async Task NavigateToLogMealAsync(int foodId)
        {
            SetNavigationParameter("PreselectedFoodId", foodId);
            await Shell.Current.GoToAsync("//LogMeal");
        }

        public async Task NavigateToProfileAsync()
        {
            await Shell.Current.GoToAsync("//Profile");
        }

        public async Task NavigateToHistoryAsync()
        {
            await Shell.Current.GoToAsync("//History");
        }

        public async Task NavigateToHistoryDetailAsync(DateTime date)
        {
            SetNavigationParameter("SelectedDate", date);
            await Shell.Current.GoToAsync("//History/Detail");
        }

        public async Task NavigateToFoodSearchAsync()
        {
            await Shell.Current.GoToAsync("//FoodSearch");
        }

        public async Task NavigateToWeightLogAsync()
        {
            await Shell.Current.GoToAsync("//WeightLog");
        }

        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        public T GetNavigationParameter<T>(string key)
        {
            if (_parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default!;
        }

        public void SetNavigationParameter(string key, object value)
        {
            _parameters[key] = value;
        }

        public void ClearNavigationParameters()
        {
            _parameters.Clear();
        }
    }
}
