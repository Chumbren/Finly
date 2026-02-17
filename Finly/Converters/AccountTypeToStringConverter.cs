using System.Globalization;
using Microsoft.Maui.Controls;
using Finly.Models;

namespace Finly.Converters
{
    public class AccountTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AccountType type)
            {
                return type switch
                {
                    AccountType.Cash => "Наличные",
                    AccountType.BankAccount => "Банковская карта",
                    AccountType.CreditCard => "Кредитная карта",
                    AccountType.Investment => "Инвестиции",
                    AccountType.Loan => "Кредит",
                    _ => "Неизвестный тип"
                };
            }
            return "Неизвестный тип";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    "Наличные" => AccountType.Cash,
                    "Банковская карта" => AccountType.BankAccount,
                    "Кредитная карта" => AccountType.CreditCard,
                    "Инвестиции" => AccountType.Investment,
                    "Кредит" => AccountType.Loan,
                    _ => AccountType.Cash
                };
            }
            return AccountType.Cash;
        }
    }
}