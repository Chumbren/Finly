using System.Globalization;

namespace Finly.Converters
{
    public class BoolToStatusColorConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isActive && isActive) ? "#2E7D32" : "#C62828";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}