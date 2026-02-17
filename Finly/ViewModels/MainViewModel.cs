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

namespace Finly.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<DataChangedMessage>
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private decimal _totalBalance;

        [ObservableProperty]
        private decimal _todayIncome;

        [ObservableProperty]
        private decimal _todayExpenses;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<TransactionDisplayItem> _recentTransactionsDisplay = new();

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

                // Проверяем все транзакции в БД
                var allTransactions = await _dataService.GetTransactionsAsync();
                Debug.WriteLine($"Всего транзакций в БД: {allTransactions.Count}");

                foreach (var t in allTransactions)
                {
                    Debug.WriteLine($"Транзакция ID:{t.Id}, Дата:{t.Date:dd.MM.yyyy}, Сумма:{t.Amount}, Тип:{t.Type}, Описание:{t.Description}");
                }

                // Загрузка последних операций (за 30 дней для теста)
                var transactions = await _dataService.GetTransactionsAsync(
                    DateTime.Today.AddDays(-30),
                    DateTime.Today.AddDays(1)); // +1 день чтобы включить сегодня

                Debug.WriteLine($"Транзакций за 30 дней: {transactions?.Count ?? 0}");

                // Загрузка всех категорий и счетов для сопоставления
                var categories = await _dataService.GetCategoriesAsync();
                var accounts = await _dataService.GetAccountsAsync();

                Debug.WriteLine($"Категорий: {categories?.Count ?? 0}, Счетов: {accounts?.Count ?? 0}");

                // Очищаем существующую коллекцию
                RecentTransactionsDisplay.Clear();

                foreach (var transaction in transactions)
                {
                    var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryId)
                                ?? new Category { Name = "Без категории", Icon = "❓", Color = "#9E9E9E" };

                    var account = accounts.FirstOrDefault(a => a.Id == transaction.AccountId)
                               ?? new Account { Name = "Неизвестный счет" };

                    RecentTransactionsDisplay.Add(new TransactionDisplayItem
                    {
                        Transaction = transaction,
                        Category = category,
                        Account = account
                    });

                    Debug.WriteLine($"Добавлен элемент: {transaction.Description} - {transaction.Amount} ({category.Name})");
                }

                Debug.WriteLine($"Отображаемых элементов: {RecentTransactionsDisplay.Count}");

                // Расчет общего баланса (только активные счета)
                TotalBalance = accounts.Where(a => a.IsActive).Sum(a => a.Balance);

                // Сегодняшняя статистика
                TodayIncome = await _dataService.GetTotalIncomeAsync(DateTime.Today, DateTime.Today);
                TodayExpenses = await _dataService.GetTotalExpensesAsync(DateTime.Today, DateTime.Today);

                Debug.WriteLine($"Баланс: {TotalBalance}, Доход сегодня: {TodayIncome}, Расход сегодня: {TodayExpenses}");
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

        // НОВЫЙ МЕТОД: Редактирование транзакции
        [RelayCommand]
        private async Task EditTransaction(TransactionDisplayItem displayItem)
        {
            if (displayItem?.Transaction == null) return;

            Debug.WriteLine($"EditTransaction: {displayItem.Description} (ID: {displayItem.Transaction.Id})");
            await Shell.Current.GoToAsync($"{nameof(AddTransactionPage)}?TransactionId={displayItem.Transaction.Id}");
        }

        // НОВЫЙ МЕТОД: Удаление транзакции с подтверждением
        [RelayCommand]
        private async Task DeleteTransaction(TransactionDisplayItem displayItem)
        {
            if (displayItem?.Transaction == null) return;

            // Запрашиваем подтверждение
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

                // Удаляем через DataService
                int result = await _dataService.DeleteTransactionAsync(displayItem.Transaction.Id);

                if (result > 0)
                {
                    Debug.WriteLine("Транзакция успешно удалена");

                    // Отправляем сообщение об изменении данных
                    WeakReferenceMessenger.Default.Send(new DataChangedMessage
                    {
                        EntityType = "Transaction",
                        EntityId = displayItem.Transaction.Id,
                        ChangeType = ChangeType.Deleted
                    });

                    // Показываем уведомление об успехе
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