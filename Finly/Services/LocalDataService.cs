
using Finly.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Account = Finly.Models.Account;

namespace Finly.Services
{
    public class LocalDataService : IDataService
    {
        private readonly SQLiteAsyncConnection _database;
        private bool _isInitialized;

        public LocalDataService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "finly.db3");
            Debug.WriteLine($"Путь к БД: {dbPath}");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await _database.CreateTableAsync<Transaction>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<Account>();
                await _database.CreateTableAsync<Budget>();

                await EnsureDefaultDataAsync();

                _isInitialized = true;
                Debug.WriteLine("База данных инициализирована успешно");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации БД: {ex}");
            }
        }

        private async Task EnsureDefaultDataAsync()
        {
            // Categories
            var categoryCount = await _database.Table<Category>().CountAsync();
            if (categoryCount == 0)
            {
                await _database.InsertAllAsync(new List<Category>
                {
                    new Category { Name = "Зарплата", Type = CategoryType.Income, Icon = "💼", Color = "#4CAF50", IsFavorite = true },
                    new Category { Name = "Премия", Type = CategoryType.Income, Icon = "🎁", Color = "#8BC34A" },
                    new Category { Name = "Продукты", Type = CategoryType.Expense, Icon = "🛒", Color = "#F44336", IsFavorite = true },
                    new Category { Name = "Транспорт", Type = CategoryType.Expense, Icon = "🚗", Color = "#2196F3", IsFavorite = true },
                    new Category { Name = "Развлечения", Type = CategoryType.Expense, Icon = "🎬", Color = "#FF9800" },
                    new Category { Name = "Коммунальные услуги", Type = CategoryType.Expense, Icon = "🏠", Color = "#9C27B0", IsFavorite = true }
                });
                Debug.WriteLine("Добавлены категории по умолчанию");
            }

            // Accounts
            var accountCount = await _database.Table<Account>().CountAsync();
            if (accountCount == 0)
            {
                await _database.InsertAllAsync(new List<Account>
                {
                    new Account { Name = "Наличные", Type = AccountType.Cash, Currency = "RUB", Balance = 0, IsPrimary = true, IsActive = true, LastUpdated = DateTime.Now },
                    new Account { Name = "Карта Сбербанк", Type = AccountType.BankAccount, Currency = "RUB", Balance = 0, BankName = "Сбербанк", AccountNumber = "****1234", IsPrimary = false, IsActive = true, LastUpdated = DateTime.Now }
                });
                Debug.WriteLine("Добавлены счета по умолчанию");
            }
        }

        public async Task<ObservableCollection<Transaction>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _database.Table<Transaction>().OrderByDescending(t => t.Date);

                if (startDate.HasValue)
                    query = query.Where(t => t.Date >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(t => t.Date <= endDate.Value);

                var items = await query.ToListAsync();
                Debug.WriteLine($"GetTransactionsAsync: получено {items.Count} транзакций");
                return new ObservableCollection<Transaction>(items);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка GetTransactionsAsync: {ex}");
                return [];
            }
        }

        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            try
            {
                return await _database.Table<Transaction>().FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка GetTransactionByIdAsync: {ex}");
                return null;
            }
        }

        public async Task<int> AddTransactionAsync(Transaction transaction)
        {
            try
            {
                Debug.WriteLine($"Добавление операции: {transaction.Description}, сумма: {transaction.Amount}");

                if (transaction.Date == DateTime.MinValue)
                    transaction.Date = DateTime.Now;

                // Сохраняем транзакцию
                var result = await _database.InsertAsync(transaction);
                Debug.WriteLine($"Транзакция добавлена, результат InsertAsync: {result}, ID транзакции: {transaction.Id}");

                // Обновляем баланс счета
                var account = await _database.Table<Account>().FirstOrDefaultAsync(a => a.Id == transaction.AccountId);
                if (account != null)
                {
                    var balanceChange = transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
                    account.Balance += balanceChange;
                    account.LastUpdated = DateTime.Now;
                    await _database.UpdateAsync(account);
                    Debug.WriteLine($"Баланс счета {account.Name} обновлен: {account.Balance}");
                }

                return transaction.Id; // Возвращаем ID созданной транзакции
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА добавления транзакции: {ex}");
                return 0;
            }
        }

        public async Task<int> UpdateTransactionAsync(Transaction transaction)
        {
            try
            {
                Debug.WriteLine($"Обновление операции ID: {transaction.Id}");

                var oldTransaction = await GetTransactionByIdAsync(transaction.Id);

                if (oldTransaction != null)
                {
                    // Если изменился счет или сумма, корректируем балансы
                    if (oldTransaction.AccountId != transaction.AccountId ||
                        oldTransaction.Amount != transaction.Amount ||
                        oldTransaction.Type != transaction.Type)
                    {
                        // Восстанавливаем баланс старого счета
                        var oldAccount = await GetAccountByIdAsync(oldTransaction.AccountId);
                        if (oldAccount != null)
                        {
                            var oldBalanceChange = oldTransaction.Type == TransactionType.Income
                                ? oldTransaction.Amount
                                : -oldTransaction.Amount;
                            oldAccount.Balance -= oldBalanceChange;
                            await _database.UpdateAsync(oldAccount);
                            Debug.WriteLine($"Старый счет {oldAccount.Name} восстановлен: {oldAccount.Balance}");
                        }

                        // Обновляем баланс нового счета
                        var newAccount = await GetAccountByIdAsync(transaction.AccountId);
                        if (newAccount != null)
                        {
                            var newBalanceChange = transaction.Type == TransactionType.Income
                                ? transaction.Amount
                                : -transaction.Amount;
                            newAccount.Balance += newBalanceChange;
                            await _database.UpdateAsync(newAccount);
                            Debug.WriteLine($"Новый счет {newAccount.Name} обновлен: {newAccount.Balance}");
                        }
                    }
                }

                var result = await _database.UpdateAsync(transaction);
                Debug.WriteLine($"Транзакция обновлена, результат: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА обновления транзакции: {ex}");
                return 0;
            }
        }

        public async Task<int> DeleteTransactionAsync(int id)
        {
            try
            {
                Debug.WriteLine($"Удаление операции ID: {id}");

                var transaction = await _database.Table<Transaction>().FirstOrDefaultAsync(t => t.Id == id);
                if (transaction != null)
                {
                    // Восстанавливаем баланс счета
                    var account = await _database.Table<Account>().FirstOrDefaultAsync(a => a.Id == transaction.AccountId);
                    if (account != null)
                    {
                        var balanceChange = transaction.Type == TransactionType.Income
                            ? transaction.Amount
                            : -transaction.Amount;
                        account.Balance -= balanceChange;
                        account.LastUpdated = DateTime.Now;
                        await _database.UpdateAsync(account);
                        Debug.WriteLine($"Баланс счета {account.Name} восстановлен: {account.Balance}");
                    }
                }

                var result = await _database.DeleteAsync<Transaction>(id);
                Debug.WriteLine($"Транзакция удалена, результат: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА удаления транзакции: {ex}");
                return 0;
            }
        }

        // Остальные методы остаются без изменений...
        public async Task<ObservableCollection<Category>> GetCategoriesAsync(CategoryType? type = null)
        {
            try
            {
                var query = _database.Table<Category>();
                if (type.HasValue)
                    query = query.Where(c => c.Type == type.Value);

                var items = await query.ToListAsync();
                return new ObservableCollection<Category>(items);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка GetCategoriesAsync: {ex}");
                return [];
            }
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _database.Table<Category>().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> AddCategoryAsync(Category category)
        {
            return await _database.InsertAsync(category);
        }

        public async Task<int> UpdateCategoryAsync(Category category)
        {
            return await _database.UpdateAsync(category);
        }

        public async Task<int> DeleteCategoryAsync(int id)
        {
            return await _database.DeleteAsync<Category>(id);
        }

        public async Task<ObservableCollection<Account>> GetAccountsAsync()
        {
            try
            {
                var items = await _database.Table<Account>().ToListAsync();
                Debug.WriteLine($"GetAccountsAsync: получено {items.Count} счетов");
                foreach (var account in items)
                {
                    Debug.WriteLine($"Счет: {account.Name}, Баланс: {account.Balance}, Активен: {account.IsActive}");
                }
                return new ObservableCollection<Account>(items);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка GetAccountsAsync: {ex}");
                return new ObservableCollection<Account>();
            }
        }

        public async Task<Account> GetAccountByIdAsync(int id)
        {
            return await _database.Table<Account>().FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<int> AddAccountAsync(Account account)
        {
            if (account.IsPrimary)
            {
                var accounts = await _database.Table<Account>().Where(a => a.IsPrimary).ToListAsync();
                foreach (var acc in accounts)
                {
                    acc.IsPrimary = false;
                    await _database.UpdateAsync(acc);
                }
            }

            return await _database.InsertAsync(account);
        }

        public async Task<int> UpdateAccountAsync(Account account)
        {
            if (account.IsPrimary)
            {
                var accounts = await _database.Table<Account>().Where(a => a.IsPrimary && a.Id != account.Id).ToListAsync();
                foreach (var acc in accounts)
                {
                    acc.IsPrimary = false;
                    await _database.UpdateAsync(acc);
                }
            }

            return await _database.UpdateAsync(account);
        }

        public async Task<int> DeleteAccountAsync(int id)
        {
            return await _database.DeleteAsync<Account>(id);
        }

        public async Task<ObservableCollection<Budget>> GetBudgetsAsync()
        {
            var items = await _database.Table<Budget>().ToListAsync();
            return new ObservableCollection<Budget>(items);
        }

        public async Task<Budget> GetBudgetByIdAsync(int id)
        {
            return await _database.Table<Budget>().FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<int> AddBudgetAsync(Budget budget)
        {
            return await _database.InsertAsync(budget);
        }

        public async Task<int> UpdateBudgetAsync(Budget budget)
        {
            return await _database.UpdateAsync(budget);
        }

        public async Task<int> DeleteBudgetAsync(int id)
        {
            return await _database.DeleteAsync<Budget>(id);
        }

        public async Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate)
        {
            // Включаем весь день до 23:59:59
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

            var transactions = await _database.Table<Transaction>()
                .Where(t => t.Type == TransactionType.Income &&
                           t.Date >= startDate.Date &&
                           t.Date <= endOfDay)
                .ToListAsync();

            return transactions.Sum(t => t.Amount);
        }

        public async Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate)
        {
            // Включаем весь день до 23:59:59
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

            var transactions = await _database.Table<Transaction>()
                .Where(t => t.Type == TransactionType.Expense &&
                           t.Date >= startDate.Date &&
                           t.Date <= endOfDay)
                .ToListAsync();

            return transactions.Sum(t => t.Amount);
        }
        public async Task<bool> CheckDatabaseConnection()
        {
            try
            {
                var result = await _database.ExecuteScalarAsync<int>("SELECT 1");
                Debug.WriteLine($"Подключение к БД успешно: {result}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА подключения к БД: {ex}");
                return false;
            }
        }
    }
}