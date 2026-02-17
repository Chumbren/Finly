using System.Globalization;

namespace Finly.Converters
{
    public class BoolToStatusLightColorConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isActive && isActive) ? "#E8F5E9" : "#FFEBEE";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}