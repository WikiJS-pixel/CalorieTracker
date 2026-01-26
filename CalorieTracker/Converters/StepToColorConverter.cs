using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace CalorieTracker.Converters
{
    public class StepToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int current && parameter != null && int.TryParse(parameter.ToString(), out int step))
            {
                // Completed or current step = accent color, future = light gray
                return current >= step ? Color.FromArgb("#512BD4") : Colors.LightGray;
            }
            return Colors.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
