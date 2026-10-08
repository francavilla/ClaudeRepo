using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PasswordGen.Converters
{
    /// <summary>true → Collapsed, false → Visible (es. testo segnaposto quando non c'è un progetto).</summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBooleanToVisibilityConverter Instance = new InverseBooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
