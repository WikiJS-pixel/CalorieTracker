using System.Globalization;

namespace CalorieTracker.Converters
{
    public class MacroProgressConverter : IValueConverter, IMultiValueConverter
    {
        // For single value binding (legacy support)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double consumed && parameter is double goal && goal > 0)
            {
                return consumed / goal;
            }
            return 0.0;
        }

        // For multi-value binding
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 2 && values[0] is double consumed && values[1] is double goal && goal > 0)
            {
                return consumed / goal;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
