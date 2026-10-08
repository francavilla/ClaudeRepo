using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExeBuilder.Converters
{
    /// <summary>Visible se il valore (es. un enum) corrisponde al parametro, altrimenti Collapsed.</summary>
    public sealed class EqualsToVisibilityConverter : IValueConverter
    {
        public static readonly EqualsToVisibilityConverter Instance = new EqualsToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && parameter != null && string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
