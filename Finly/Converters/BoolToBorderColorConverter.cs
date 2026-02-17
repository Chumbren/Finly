using System.Globalization;

namespace Finly.Converters
{
    public class BoolToBorderColorConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isActive && isActive) ? "#4A148C" : "#E0E0E0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}