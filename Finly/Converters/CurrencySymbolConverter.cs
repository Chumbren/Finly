using System.Globalization;

namespace Finly.Converters
{
    public class CurrencySymbolConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "₽";

            var currencyCode = value.ToString();

            return currencyCode switch
            {
                "RUB" => "₽",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                "JPY" => "¥",
                "CNY" => "¥",
                _ => currencyCode
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}