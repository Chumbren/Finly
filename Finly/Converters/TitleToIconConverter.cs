// Добавьте в папку Converters

using System.Globalization;

public class TitleToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string title = value as string;
        return title?.Contains("Редактирование") == true ? "✏️" : "➕";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InvertedBoolToColumnConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Для кнопки Сохранить: если IsEditMode=true, то колонка 1, иначе колонка 0 (при одной кнопке растягиваем на две)
        if (value is bool isEditMode)
        {
            return isEditMode ? 1 : 0;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}