using System.Globalization;

namespace BinSoundTech.Converters;

/// <summary>
/// Converts a boolean value to button text for microphone monitoring toggle.
/// True = "Stop Monitoring", False = "Start Monitoring"
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Stop Monitoring" : "Start Monitoring";
        }
        return "Start Monitoring";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
