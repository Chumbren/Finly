using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Finly.Converters
{
    public class ColorToLightColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorHex && !string.IsNullOrEmpty(colorHex))
            {
                try
                {
                    var color = Color.FromArgb(colorHex);
                    return color.WithLuminosity(0.95f); // Светлый фон
                }
                catch
                {
                    return Color.FromArgb("#F5F5F5");
                }
            }
            return Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
