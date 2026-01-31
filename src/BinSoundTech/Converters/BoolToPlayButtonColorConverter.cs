using System.Globalization;

namespace BinSoundTech.Converters;

/// <summary>
/// Converts a boolean (IsActive) to play button color.
/// True (playing) = Green, False (stopped) = Default
/// </summary>
public class BoolToPlayButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            // Green color when playing
            return Color.FromArgb("#4CAF50");
        }
        
        // Default button color when not playing
        if (Application.Current?.Resources.TryGetValue("Primary", out var primaryColor) == true)
        {
            return primaryColor;
        }
        return Colors.Purple;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
