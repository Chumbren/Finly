using System.Globalization;

namespace Finly.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Если значение null, возвращаем false
            if (value == null)
                return false;

            string stringValue = value.ToString() ?? string.Empty;

            // Если параметр не передан, просто проверяем на пустоту
            if (parameter == null)
                return !string.IsNullOrWhiteSpace(stringValue);

            // Если параметр передан, сравниваем с ним
            string paramString = parameter.ToString() ?? string.Empty;

            // Поддержка множественных значений через запятую
            if (paramString.Contains(','))
            {
                var options = paramString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return options.Any(opt => opt.Trim().Equals(stringValue, StringComparison.OrdinalIgnoreCase));
            }

            return stringValue.Equals(paramString, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Если значение не булево или false - возвращаем пустую строку
            if (value is not bool boolValue || !boolValue)
                return string.Empty;

            // Если параметр не передан - возвращаем пустую строку
            if (parameter == null)
                return string.Empty;

            string paramString = parameter.ToString() ?? string.Empty;

            // Если target type - string, возвращаем строку
            if (targetType == typeof(string))
                return paramString;

            // Если target type - object, тоже возвращаем строку
            return paramString;
        }
    }
}