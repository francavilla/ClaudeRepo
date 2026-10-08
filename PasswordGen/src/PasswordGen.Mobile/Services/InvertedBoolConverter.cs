using System.Globalization;

namespace PasswordGen.Mobile.Services;

/// <summary>true -> false e viceversa (per mostrare un testo quando un elenco è vuoto).</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value is bool flag && flag);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
