using SQLite;
using System;

namespace Finly.Models
{
    public class Account
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Currency { get; set; } = "RUB";

        public AccountType Type { get; set; }

        public decimal Balance { get; set; }

        public string? BankName { get; set; }

        public string? AccountNumber { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public enum AccountType
    {
        Cash,
        BankAccount,
        CreditCard,
        Investment,
        Loan
    }
}