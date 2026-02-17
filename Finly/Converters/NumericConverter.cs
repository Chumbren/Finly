using System.Globalization;
using System.Text.RegularExpressions;

namespace Finly.Converters
{
    public class NumericConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("F2", culture);
            }
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // Очищаем строку от всех символов, кроме цифр и точки
                string cleanedValue = Regex.Replace(stringValue, @"[^\d.]", "");

                // Убираем лишние точки
                int dotIndex = cleanedValue.IndexOf('.');
                if (dotIndex >= 0)
                {
                    string beforeDot = cleanedValue.Substring(0, dotIndex);
                    string afterDot = cleanedValue.Substring(dotIndex + 1).Replace(".", "");
                    cleanedValue = beforeDot + "." + afterDot;

                    // Ограничиваем количество знаков после точки до 2
                    if (afterDot.Length > 2)
                    {
                        afterDot = afterDot.Substring(0, 2);
                        cleanedValue = beforeDot + "." + afterDot;
                    }
                }

                if (string.IsNullOrEmpty(cleanedValue) || cleanedValue == ".")
                {
                    return 0m;
                }

                if (decimal.TryParse(cleanedValue, NumberStyles.Any, culture, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}