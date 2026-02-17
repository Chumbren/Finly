using System.Globalization;

namespace Finly.Converters
{
    public class DescriptionValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string description)
            {
                return string.IsNullOrWhiteSpace(description);
            }
            return true; // Если значение не строка - показываем ошибку
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}