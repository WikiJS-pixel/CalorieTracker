using System.Globalization;

namespace CalorieTracker.Converters
{
    public class RemainingCaloriesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double remaining = (double)value;
            double abs = Math.Abs(remaining);
            return remaining >= 0 ? $"{abs:F0} kcal left" : $"{abs:F0} kcal over";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
