using SQLite;
using System;

namespace Finly.Models
{
    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public int AccountId { get; set; }

        public TransactionType Type { get; set; } = TransactionType.Expense;

        public string? Notes { get; set; }

        public bool IsRecurring { get; set; }

        public RecurrenceType? RecurrencePattern { get; set; }

        public DateTime? NextOccurrence { get; set; }
    }

    public enum TransactionType
    {
        Income,
        Expense,
        Transfer
    }

    public enum RecurrenceType
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }
}