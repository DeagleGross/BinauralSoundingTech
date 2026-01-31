using System.Globalization;

namespace BinSoundTech.Converters;

/// <summary>
/// Converts a boolean value to a button background color.
/// True = Primary color (selected), False = Gray (unselected)
/// </summary>
public class BoolToButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            // Return primary color for selected state
            if (Application.Current?.Resources.TryGetValue("Primary", out var primaryColor) == true)
            {
                return primaryColor;
            }
            return Colors.Purple;
        }
        
        // Return gray for unselected state
        return Color.FromArgb("#808080");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
