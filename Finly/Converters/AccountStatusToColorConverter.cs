using System.Globalization;
using Microsoft.Maui.Controls;

namespace Finly.Converters
{
    public class AccountStatusToColorConverter : IValueConverter
    {
        public object Convert(object?  value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Colors.White : Color.FromArgb("#F5F5F5");
            }
            return Colors.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}