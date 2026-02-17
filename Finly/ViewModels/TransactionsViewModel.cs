using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Account = Finly.Models.Account;

namespace Finly.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<Transaction> _transactions = new();

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
        }

        [RelayCommand]
        private async Task LoadData()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Categories = await _dataService.GetCategoriesAsync();
                Accounts = await _dataService.GetAccountsAsync();
                await LoadTransactions();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadTransactions()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var transactions = await _dataService.GetTransactionsAsync(StartDate, EndDate);

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    transactions = new ObservableCollection<Transaction>(
                        transactions.Where(t => t.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (SelectedCategory != null)
                {
                    transactions = new ObservableCollection<Transaction>(
                        transactions.Where(t => t.CategoryId == SelectedCategory.Id)
                    );
                }

                if (SelectedAccount != null)
                {
                    transactions = new ObservableCollection<Transaction>(
                        transactions.Where(t => t.AccountId == SelectedAccount.Id)
                    );
                }

                Transactions = transactions;

                TotalIncome = await _dataService.GetTotalIncomeAsync(StartDate, EndDate);
                TotalExpenses = await _dataService.GetTotalExpensesAsync(StartDate, EndDate);
                NetBalance = TotalIncome - TotalExpenses;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddTransaction()
        {
            await Shell.Current.GoToAsync($"///{nameof(AddTransactionPage)}");
        }

        [RelayCommand]
        private async Task EditTransaction(Transaction transaction)
        {
            if (transaction != null)
            {
                await Shell.Current.GoToAsync($"{nameof(AddTransactionPage)}?TransactionId={transaction.Id}");
            }
        }

        [RelayCommand]
        private async Task DeleteTransaction(Transaction transaction)
        {
            if (transaction == null) return;

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение",
                $"Удалить операцию '{transaction.Description}' на сумму {transaction.Amount:C}?",
                "Да", "Нет");

            if (confirm)
            {
                await _dataService.DeleteTransactionAsync(transaction.Id);
                await LoadTransactions();
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            _ = LoadTransactions();
        }

        partial void OnSelectedCategoryChanged(Category value)
        {
            _ = LoadTransactions();
        }

        partial void OnSelectedAccountChanged(Account value)
        {
            _ = LoadTransactions();
        }

        partial void OnStartDateChanged(DateTime value)
        {
            if (value > EndDate)
                EndDate = value;
            _ = LoadTransactions();
        }

        partial void OnEndDateChanged(DateTime value)
        {
            if (value < StartDate)
                StartDate = value;
            _ = LoadTransactions();
        }
    }
}