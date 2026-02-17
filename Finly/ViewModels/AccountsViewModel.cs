
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using System;
using System.Collections.ObjectModel;
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

                // Важно: очищаем и добавляем по одному для обновления UI
                Accounts.Clear();
                foreach (var account in accounts)
                {
                    Accounts.Add(account);
                    System.Diagnostics.Debug.WriteLine($"  Добавлен счет: {account.Name}, Баланс: {account.Balance}");
                }

                UpdateTotalBalance();

                // Принудительно обновляем привязки
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

        

        [RelayCommand]
        private async Task DeleteAccount(Account account)
        {
            if (account == null) return;

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение",
                $"Удалить счет '{account.Name}'? Все операции, связанные с этим счетом, будут сохранены, но счет станет недоступен.",
                "Да", "Нет");

            if (confirm)
            {
                IsBusy = true;
                try
                {
                    await _dataService.DeleteAccountAsync(account.Id);
                    await Shell.Current.DisplayAlertAsync("Успех", "Счет удален", "OK");
                    await LoadAccountsCommand.ExecuteAsync(null);
                }
                catch (Exception ex)
                {
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