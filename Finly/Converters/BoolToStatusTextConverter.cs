using System.Globalization;
using Microsoft.Maui.Controls;

namespace Finly.Converters
{
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Активный" : "Неактивный";
            }
            return "Неизвестно";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}