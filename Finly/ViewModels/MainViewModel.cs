
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Account = Finly.Models.Account;

namespace Finly.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<DataChangedMessage>
    {
        private readonly IDataService _dataService;
        private Account _selectedAccount;

        [ObservableProperty]
        private bool _isPopupVisible;

        [RelayCommand]
        private void ShowAccountSelector()
        {
            IsPopupVisible = true;
        }

        [RelayCommand]
        private void ClosePopup()
        {
            IsPopupVisible = false;
        }

        [RelayCommand]
        private async Task SelectAccount(Account account)
        {
            CurrentAccount = account;
            await LoadDataForSelectedAccount();
            SelectedAccountName = account?.Name ?? "Все счета";
            IsPopupVisible = false;
        }

        [RelayCommand]
        private async Task ShowAllAccounts()
        {
            CurrentAccount = null;
            await LoadDataForSelectedAccount();
            SelectedAccountName = "Все счета";
            IsPopupVisible = false;
        }

        [ObservableProperty]
        private decimal _totalBalance;

        [ObservableProperty]
        private decimal _totalIncome;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _todayIncome;

        [ObservableProperty]
        private decimal _todayExpenses;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<TransactionDisplayItem> _recentTransactionsDisplay = new();

        [ObservableProperty]
        private ObservableCollection<Account> _accounts = new();

        [ObservableProperty]
        private Account _currentAccount;

        [ObservableProperty]
        private string _selectedAccountName = "Все счета";

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;

            // Регистрируем получателя сообщений
            WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);

            Debug.WriteLine("MainViewModel создан, подписан на DataChangedMessage");
        }

        public void Receive(DataChangedMessage message)
        {
            Debug.WriteLine($"Получено сообщение DataChangedMessage: {message.ChangeType} - {message.EntityType}");
            // Загружаем данные при получении сообщения
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDashboardDataCommand.ExecuteAsync(null);
            });
        }

        [RelayCommand]
        private async Task LoadDashboardData()
        {
            if (IsBusy)
            {
                Debug.WriteLine("LoadDashboardData: уже выполняется");
                return;
            }

            IsBusy = true;

            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА ДАННЫХ ДАШБОРДА ===");

                // Загружаем все счета
                Accounts = await _dataService.GetAccountsAsync();
                Debug.WriteLine($"Загружено счетов: {Accounts.Count}");

                // Если нет выбранного счета, показываем все
                await LoadDataForSelectedAccount();

                Debug.WriteLine($"Баланс: {TotalBalance}, Доход всего: {TotalIncome}, Расход всего: {TotalExpenses}");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadDataForSelectedAccount()
        {
            // Определяем даты для статистики "за все время" (используем минимальную дату)
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = DateTime.Today.AddDays(1);

            // Загрузка транзакций
            var allTransactions = await _dataService.GetTransactionsAsync(startDate, endDate);
            Debug.WriteLine($"Всего транзакций в БД: {allTransactions.Count}");

            // Фильтруем по выбранному счету если нужно
            var filteredTransactions = allTransactions;
            if (CurrentAccount != null)
            {
                filteredTransactions = new ObservableCollection<Models.Transaction>(
                    allTransactions.Where(t => t.AccountId == CurrentAccount.Id)
                );
                SelectedAccountName = CurrentAccount.Name;
            }
            else
            {
                SelectedAccountName = "Все счета";
            }

            Debug.WriteLine($"Транзакций после фильтрации: {filteredTransactions.Count}");

            // Загрузка всех категорий для сопоставления
            var categories = await _dataService.GetCategoriesAsync();

            // Очищаем существующую коллекцию
            RecentTransactionsDisplay.Clear();

            // Берем последние 10 транзакций
            foreach (var transaction in filteredTransactions.OrderByDescending(t => t.Date).Take(10))
            {
                var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryId)
                            ?? new Category { Name = "Без категории", Icon = "❓", Color = "#9E9E9E" };

                var account = Accounts.FirstOrDefault(a => a.Id == transaction.AccountId)
                           ?? new Account { Name = "Неизвестный счет" };

                RecentTransactionsDisplay.Add(new TransactionDisplayItem
                {
                    Transaction = transaction,
                    Category = category,
                    Account = account
                });
            }

            Debug.WriteLine($"Отображаемых элементов: {RecentTransactionsDisplay.Count}");

            // Расчет общего баланса (только активные счета)
            if (CurrentAccount != null)
            {
                TotalBalance = CurrentAccount.Balance;
            }
            else
            {
                TotalBalance = Accounts.Where(a => a.IsActive).Sum(a => a.Balance);
            }

            // Статистика за все время
            if (CurrentAccount != null)
            {
                TotalIncome = filteredTransactions
                    .Where(t => t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);
                TotalExpenses = filteredTransactions
                    .Where(t => t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);
            }
            else
            {
                TotalIncome = await _dataService.GetTotalIncomeAsync(startDate, endDate);
                TotalExpenses = await _dataService.GetTotalExpensesAsync(startDate, endDate);
            }

            // Сегодняшняя статистика
            TodayIncome = filteredTransactions
                .Where(t => t.Type == TransactionType.Income && t.Date.Date == DateTime.Today)
                .Sum(t => t.Amount);
            TodayExpenses = filteredTransactions
                .Where(t => t.Type == TransactionType.Expense && t.Date.Date == DateTime.Today)
                .Sum(t => t.Amount);
        }

       

        

        [RelayCommand]
        private async Task NavigateToPage(string pageName)
        {
            switch (pageName)
            {
                case "Transactions":
                    await Shell.Current.GoToAsync($"///{nameof(TransactionsPage)}");
                    break;
                case "Accounts":
                    await Shell.Current.GoToAsync($"///{nameof(AccountsPage)}");
                    break;
                case "Reports":
                    await Shell.Current.GoToAsync($"///{nameof(ReportsPage)}");
                    break;
            }
        }

        [RelayCommand]
        private async Task AddTransaction()
        {
            await Shell.Current.GoToAsync($"///{nameof(AddTransactionPage)}");
        }

        [RelayCommand]
        private async Task EditTransaction(TransactionDisplayItem displayItem)
        {
            if (displayItem?.Transaction == null) return;

            Debug.WriteLine($"EditTransaction: {displayItem.Description} (ID: {displayItem.Transaction.Id})");
            await Shell.Current.GoToAsync($"{nameof(AddTransactionPage)}?TransactionId={displayItem.Transaction.Id}");
        }

        [RelayCommand]
        private async Task DeleteTransaction(TransactionDisplayItem displayItem)
        {
            if (displayItem?.Transaction == null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение удаления",
                $"Вы уверены, что хотите удалить операцию '{displayItem.Description}' на сумму {displayItem.FormattedAmount}?",
                "Да",
                "Нет");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                Debug.WriteLine($"DeleteTransaction: Удаление транзакции ID {displayItem.Transaction.Id}");

                int result = await _dataService.DeleteTransactionAsync(displayItem.Transaction.Id);

                if (result > 0)
                {
                    Debug.WriteLine("Транзакция успешно удалена");

                    WeakReferenceMessenger.Default.Send(new DataChangedMessage
                    {
                        EntityType = "Transaction",
                        EntityId = displayItem.Transaction.Id,
                        ChangeType = ChangeType.Deleted
                    });

                    await Shell.Current.DisplayAlertAsync("Успех", "Операция удалена", "OK");
                }
                else
                {
                    Debug.WriteLine("Не удалось удалить транзакцию");
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось удалить операцию", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при удалении транзакции: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось удалить операцию: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    public partial class TransactionDisplayItem : ObservableObject
    {
        [ObservableProperty]
        private Transaction _transaction;

        [ObservableProperty]
        private Category _category;

        [ObservableProperty]
        private Account _account;

        // Вычисляемые свойства для привязки
        public string Description => Transaction?.Description ?? string.Empty;

        public decimal Amount => Transaction?.Amount ?? 0;

        public DateTime Date => Transaction?.Date ?? DateTime.Now;

        public string CategoryIcon => Category?.Icon ?? "❓";

        public string CategoryName => Category?.Name ?? "Без категории";

        public string AccountName => Account?.Name ?? "Неизвестный счет";

        public TransactionType Type => Transaction?.Type ?? TransactionType.Expense;

        // Цвет категории для оформления
        public string CategoryColor => Category?.Color ?? "#9E9E9E";

        // Форматированная сумма со знаком
        public string FormattedAmount
        {
            get
            {
                if (Transaction == null) return "0 ₽";

                string sign = Type == TransactionType.Income ? "+" : "-";
                return $"{sign} {Amount:N2} ₽";
            }
        }

        // Цвет суммы в зависимости от типа
        public string AmountColor => Type == TransactionType.Income ? "#4CAF50" : "#F44336";

        // Обновляем вычисляемые свойства при изменении Transaction
        partial void OnTransactionChanged(Transaction value)
        {
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Amount));
            OnPropertyChanged(nameof(Date));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(FormattedAmount));
            OnPropertyChanged(nameof(AmountColor));
        }

        partial void OnCategoryChanged(Category value)
        {
            OnPropertyChanged(nameof(CategoryIcon));
            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(CategoryColor));
        }

        partial void OnAccountChanged(Account value)
        {
            OnPropertyChanged(nameof(AccountName));
        }

        // Конструктор для удобства
        public TransactionDisplayItem()
        {
            _transaction = new Transaction();
            _category = new Category();
            _account = new Account();
        }

        public TransactionDisplayItem(Transaction transaction, Category category, Account account)
        {
            _transaction = transaction;
            _category = category;
            _account = account;
        }
    }
}