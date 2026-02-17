using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Finly.Converters
{
    public class AmountColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount > 0) return Colors.Green;
                if (amount < 0) return Colors.Red;
                return Colors.Gray;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
