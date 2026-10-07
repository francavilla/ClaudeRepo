using System;
using System.Globalization;
using System.Windows.Data;

namespace BuildExe.Converters
{
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new InverseBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }
    }
}
