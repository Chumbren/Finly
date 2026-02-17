using System.Globalization;
using Microsoft.Maui.Controls;
using Finly.Models;

namespace Finly.Converters
{
    public class AccountTypeToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AccountType type)
            {
                return type switch
                {
                    AccountType.Cash => "💵",
                    AccountType.BankAccount => "🏦",
                    AccountType.CreditCard => "💳",
                    AccountType.Investment => "📈",
                    AccountType.Loan => "📉",
                    _ => "💳"
                };
            }
            return "💳";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}