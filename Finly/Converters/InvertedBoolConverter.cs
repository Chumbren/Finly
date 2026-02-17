 using System;
using System.Collections.Generic;
using System.Text;

namespace Finly.Converters
{
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && !(bool)value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && !(bool)value;
        }
    }
}
