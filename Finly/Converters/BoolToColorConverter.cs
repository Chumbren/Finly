using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Finly.Converters
{
    public class BoolToColorConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isActive && isActive) ? "White" : "#F5F5F5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
