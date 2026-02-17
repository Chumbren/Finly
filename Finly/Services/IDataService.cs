using Finly.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Finly.Services
{
    public interface IDataService
    {
        // Инициализация
        Task InitializeAsync();

        // Операции
        Task<ObservableCollection<Transaction>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<int> AddTransactionAsync(Transaction transaction);
        Task<int> UpdateTransactionAsync(Transaction transaction);
        Task<int> DeleteTransactionAsync(int id);

        // Категории
        Task<ObservableCollection<Category>> GetCategoriesAsync(CategoryType? type = null);
        Task<Category> GetCategoryByIdAsync(int id);
        Task<int> AddCategoryAsync(Category category);
        Task<int> UpdateCategoryAsync(Category category);
        Task<int> DeleteCategoryAsync(int id);

        // Счета
        Task<ObservableCollection<Account>> GetAccountsAsync();
        Task<Account> GetAccountByIdAsync(int id);
        Task<int> AddAccountAsync(Account account);
        Task<int> UpdateAccountAsync(Account account);
        Task<int> DeleteAccountAsync(int id);

        // Бюджеты
        Task<ObservableCollection<Budget>> GetBudgetsAsync();
        Task<Budget> GetBudgetByIdAsync(int id);
        Task<int> AddBudgetAsync(Budget budget);
        Task<int> UpdateBudgetAsync(Budget budget);
        Task<int> DeleteBudgetAsync(int id);

        // Статистика
        Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate);
    }
}