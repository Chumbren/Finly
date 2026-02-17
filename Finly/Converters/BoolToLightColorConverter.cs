using System.Globalization;

namespace Finly.Converters
{
    public class BoolToLightColorConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isActive && isActive) ? "#F3E5F5" : "#F0F0F0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}