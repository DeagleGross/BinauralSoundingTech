using System.Globalization;

namespace BinSoundTech.Converters;

/// <summary>
/// Converts a boolean value to an opacity value for UI display.
/// True = 1.0 (fully opaque), False = 0.3 (mostly transparent)
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 1.0 : 0.3;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
