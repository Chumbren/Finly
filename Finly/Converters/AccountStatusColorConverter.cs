using System.Globalization;
using Microsoft.Maui.Controls;

namespace Finly.Converters
{
    public class AccountStatusColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
            }
            return Color.FromArgb("#4CAF50");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}