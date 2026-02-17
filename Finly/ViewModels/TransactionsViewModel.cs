using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Account = Finly.Models.Account;

namespace Finly.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<TransactionDisplayItem> _transactions = new();

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

        [ObservableProperty]
        private ObservableCollection<Account> _accounts = new();

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        [ObservableProperty]
        private Category _selectedCategory;

        [ObservableProperty]
        private Account _selectedAccount;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private decimal _totalIncome;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _netBalance;

        [ObservableProperty]
        private bool _isBusy;

        public TransactionsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Debug.WriteLine("TransactionsViewModel создан");
        }

        [RelayCommand]
        private async Task LoadData()
        {
            Debug.WriteLine("LoadData вызван");

            if (IsBusy)
            {
                Debug.WriteLine("LoadData: IsBusy = true, пропускаем");
                return;
            }

            IsBusy = true;
            Debug.WriteLine("LoadData: начало загрузки");

            try
            {
                Debug.WriteLine("LoadData: загрузка категорий");
                Categories = await _dataService.GetCategoriesAsync();
                Debug.WriteLine($"LoadData: загружено {Categories?.Count ?? 0} категорий");

                Debug.WriteLine("LoadData: загрузка счетов");
                Accounts = await _dataService.GetAccountsAsync();
                Debug.WriteLine($"LoadData: загружено {Accounts?.Count ?? 0} счетов");

                // Загружаем транзакции - убираем проверку IsBusy внутри LoadTransactions
                await LoadTransactionsInternal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadData: ОШИБКА - {ex}");
            }
            finally
            {
                IsBusy = false;
                Debug.WriteLine("LoadData: завершено");
            }
        }

        [RelayCommand]
        private async Task LoadTransactions()
        {
            Debug.WriteLine("LoadTransactions (команда) вызван");

            if (IsBusy)
            {
                Debug.WriteLine("LoadTransactions: IsBusy = true, пропускаем");
                return;
            }

            IsBusy = true;
            await LoadTransactionsInternal();
            IsBusy = false;
        }

        // Внутренний метод без проверки IsBusy
        private async Task LoadTransactionsInternal()
        {
            Debug.WriteLine($"LoadTransactionsInternal: период с {StartDate:dd.MM.yyyy} по {EndDate:dd.MM.yyyy}");

            try
            {
                Debug.WriteLine("LoadTransactionsInternal: запрос транзакций из БД");
                var transactions = await _dataService.GetTransactionsAsync(StartDate, EndDate);
                Debug.WriteLine($"LoadTransactionsInternal: получено {transactions?.Count ?? 0} транзакций из БД");

                // Выводим каждую транзакцию для отладки
                if (transactions != null)
                {
                    foreach (var t in transactions)
                    {
                        Debug.WriteLine($"Транзакция из БД: ID={t.Id}, Дата={t.Date:dd.MM.yyyy}, Сумма={t.Amount}, Тип={t.Type}, Описание={t.Description}, CategoryId={t.CategoryId}, AccountId={t.AccountId}");
                    }
                }

                // Загружаем все категории и счета для сопоставления
                Debug.WriteLine("LoadTransactionsInternal: загрузка категорий для сопоставления");
                var categories = await _dataService.GetCategoriesAsync();
                Debug.WriteLine($"LoadTransactionsInternal: загружено {categories?.Count ?? 0} категорий");

                Debug.WriteLine("LoadTransactionsInternal: загрузка счетов для сопоставления");
                var accounts = await _dataService.GetAccountsAsync();
                Debug.WriteLine($"LoadTransactionsInternal: загружено {accounts?.Count ?? 0} счетов");

                // Применяем фильтры
                var filteredTransactions = transactions.AsEnumerable();
                Debug.WriteLine($"LoadTransactionsInternal: начальная фильтрация, элементов: {filteredTransactions.Count()}");

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    filteredTransactions = filteredTransactions.Where(t =>
                        t.Description != null && t.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                    Debug.WriteLine($"LoadTransactionsInternal: после фильтра по поиску '{SearchQuery}': {filteredTransactions.Count()}");
                }

                if (SelectedCategory != null)
                {
                    filteredTransactions = filteredTransactions.Where(t =>
                        t.CategoryId == SelectedCategory.Id);
                    Debug.WriteLine($"LoadTransactionsInternal: после фильтра по категории '{SelectedCategory.Name}': {filteredTransactions.Count()}");
                }

                if (SelectedAccount != null)
                {
                    filteredTransactions = filteredTransactions.Where(t =>
                        t.AccountId == SelectedAccount.Id);
                    Debug.WriteLine($"LoadTransactionsInternal: после фильтра по счету '{SelectedAccount.Name}': {filteredTransactions.Count()}");
                }

                // Преобразуем в TransactionDisplayItem
                var displayItems = new ObservableCollection<TransactionDisplayItem>();
                Debug.WriteLine("LoadTransactionsInternal: преобразование в TransactionDisplayItem");

                foreach (var transaction in filteredTransactions)
                {
                    var category = categories?.FirstOrDefault(c => c.Id == transaction.CategoryId)
                                ?? new Category { Name = "Без категории", Icon = "❓", Color = "#9E9E9E" };

                    var account = accounts?.FirstOrDefault(a => a.Id == transaction.AccountId)
                               ?? new Account { Name = "Неизвестный счет" };

                    var displayItem = new TransactionDisplayItem
                    {
                        Transaction = transaction,
                        Category = category,
                        Account = account
                    };

                    displayItems.Add(displayItem);
                    Debug.WriteLine($"Добавлен DisplayItem: {displayItem.Description}, Сумма={displayItem.Amount}, Категория={displayItem.CategoryName}, Счет={displayItem.AccountName}");
                }

                Debug.WriteLine($"LoadTransactionsInternal: итого DisplayItems: {displayItems.Count}");

                // Присваиваем коллекцию
                Transactions = displayItems;
                Debug.WriteLine($"LoadTransactionsInternal: свойство Transactions обновлено, Count = {Transactions?.Count ?? 0}");

                // Обновляем статистику
                Debug.WriteLine("LoadTransactionsInternal: расчет статистики");
                TotalIncome = await _dataService.GetTotalIncomeAsync(StartDate, EndDate);
                TotalExpenses = await _dataService.GetTotalExpensesAsync(StartDate, EndDate);
                NetBalance = TotalIncome - TotalExpenses;
                Debug.WriteLine($"LoadTransactionsInternal: статистика - Доход={TotalIncome}, Расход={TotalExpenses}, Баланс={NetBalance}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadTransactionsInternal: ОШИБКА - {ex}");
            }
        }

        [RelayCommand]
        private async Task AddTransaction()
        {
            Debug.WriteLine("AddTransaction вызван");
            await Shell.Current.GoToAsync($"///{nameof(AddTransactionPage)}");
        }

        [RelayCommand]
        private async Task EditTransaction(TransactionDisplayItem displayItem)
        {
            Debug.WriteLine($"EditTransaction вызван: {displayItem?.Description}");
            if (displayItem?.Transaction != null)
            {
                await Shell.Current.GoToAsync($"{nameof(AddTransactionPage)}?TransactionId={displayItem.Transaction.Id}");
            }
        }

        [RelayCommand]
        private async Task DeleteTransaction(TransactionDisplayItem displayItem)
        {
            Debug.WriteLine($"DeleteTransaction вызван: {displayItem?.Description}");
            if (displayItem?.Transaction == null) return;

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение",
                $"Удалить операцию '{displayItem.Description}' на сумму {displayItem.Amount:C}?",
                "Да", "Нет");

            if (confirm)
            {
                await _dataService.DeleteTransactionAsync(displayItem.Transaction.Id);
                await LoadTransactions(); // Используем команду для перезагрузки
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            Debug.WriteLine($"SearchQuery изменен: '{value}'");
            _ = LoadTransactions();
        }

        partial void OnSelectedCategoryChanged(Category value)
        {
            Debug.WriteLine($"SelectedCategory изменен: '{value?.Name}'");
            _ = LoadTransactions();
        }

        partial void OnSelectedAccountChanged(Account value)
        {
            Debug.WriteLine($"SelectedAccount изменен: '{value?.Name}'");
            _ = LoadTransactions();
        }

        partial void OnStartDateChanged(DateTime value)
        {
            Debug.WriteLine($"StartDate изменен: {value:dd.MM.yyyy}");
            if (value > EndDate)
                EndDate = value;
            _ = LoadTransactions();
        }

        partial void OnEndDateChanged(DateTime value)
        {
            Debug.WriteLine($"EndDate изменен: {value:dd.MM.yyyy}");
            if (value < StartDate)
                StartDate = value;
            _ = LoadTransactions();
        }
    }
}