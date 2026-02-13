using System.Globalization;

namespace CalorieTracker.Converters
{
    public class ProgressToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            if (parameter?.ToString() == "Color")
                return progress >= 1.0 ? Colors.Red : Color.FromArgb("#2196F3"); // Blue → Red if over

            return progress * 360 - 90; // Start from top (12 o'clock)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
