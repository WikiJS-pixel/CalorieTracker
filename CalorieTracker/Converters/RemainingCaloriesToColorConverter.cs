using System.Globalization;

namespace CalorieTracker.Converters
{
    public class RemainingCaloriesToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double remaining = (double)value;
            return remaining >= 0 ? Colors.Green : Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
