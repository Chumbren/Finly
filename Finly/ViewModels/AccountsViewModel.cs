using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Finly.ViewModels
{
    public partial class AccountsViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private Account _editingAccount;

        [ObservableProperty]
        private ObservableCollection<Account> _accounts = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _accountName = string.Empty;

        [ObservableProperty]
        private AccountType _accountType = AccountType.Cash;

        [ObservableProperty]
        private string _currency = "RUB";

        [ObservableProperty]
        private decimal _initialBalance;

        [ObservableProperty]
        private string _bankName = string.Empty;

        [ObservableProperty]
        private string _accountNumber = string.Empty;

        [ObservableProperty]
        private bool _isPrimary;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private decimal _totalBalance;

        [ObservableProperty]
        private ObservableCollection<string> _accountTypes =
        [
          "Наличные",
          "Банковская карта",
          "Кредитная карта",
          "Инвестиции",
          "Кредит"
        ];

        public AccountsViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        [RelayCommand]
        private async Task LoadAccounts()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var accounts = await _dataService.GetAccountsAsync();
                System.Diagnostics.Debug.WriteLine($"LoadAccounts: получено {accounts.Count} счетов");

                Accounts.Clear();
                foreach (var account in accounts)
                {
                    Accounts.Add(account);
                    System.Diagnostics.Debug.WriteLine($"  Добавлен счет: {account.Name}, Баланс: {account.Balance}");
                }

                UpdateTotalBalance();

                OnPropertyChanged(nameof(Accounts));
                OnPropertyChanged(nameof(TotalBalance));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка LoadAccounts: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateTotalBalance()
        {
            TotalBalance = Accounts.Where(a => a.IsActive).Sum(a => a.Balance);
        }

        [RelayCommand]
        private void StartAddAccount()
        {
            IsEditMode = false;
        }

        [RelayCommand]
        private void StartEditAccount(Account account)
        {
            if (account == null) return;

            _editingAccount = account;
            AccountName = account.Name;
            AccountType = account.Type;
            Currency = account.Currency;
            InitialBalance = account.Balance;
            BankName = account.BankName ?? string.Empty;
            AccountNumber = account.AccountNumber ?? string.Empty;
            IsPrimary = account.IsPrimary;
            IsActive = account.IsActive;
            IsEditMode = true;
        }

        // ИЗМЕНЕННЫЙ МЕТОД: обновлено сообщение о каскадном удалении
        [RelayCommand]
        private async Task DeleteAccount(Account account)
        {
            if (account == null) return;

            // Сначала проверяем, есть ли транзакции у этого счета
            var transactions = await _dataService.GetTransactionsAsync();
            var hasTransactions = transactions.Any(t => t.AccountId == account.Id);

            string message;
            if (hasTransactions)
            {
                message = $"Удалить счет '{account.Name}'? Все операции, связанные с этим счетом ({transactions.Count(t => t.AccountId == account.Id)} шт.), также будут безвозвратно удалены.";
            }
            else
            {
                message = $"Удалить счет '{account.Name}'?";
            }

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение удаления",
                message,
                "Да", "Нет");

            if (confirm)
            {
                IsBusy = true;
                try
                {
                    var result = await _dataService.DeleteAccountAsync(account.Id);

                    if (result > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Счет и все связанные операции удалены", "OK");

                        // Отправляем сообщение об изменении данных
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage
                        {
                            EntityType = "Account",
                            EntityId = account.Id,
                            ChangeType = ChangeType.Deleted
                        });

                        await LoadAccountsCommand.ExecuteAsync(null);
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось удалить счет", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка DeleteAccount: {ex}");
                    await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось удалить счет: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task NavigateToAddAccount()
        {
            await Shell.Current.GoToAsync(nameof(AddAccountPage));
        }

        [RelayCommand]
        private async Task NavigateToEditAccount(Account account)
        {
            if (account == null) return;
            await Shell.Current.GoToAsync($"{nameof(AddAccountPage)}?AccountId={account.Id}");
        }
    }
}