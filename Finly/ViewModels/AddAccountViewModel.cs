using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Models;
using Finly.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Finly.ViewModels
{
    public partial class AddAccountViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private readonly int? _accountId;
        private Account _originalAccount;

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
        private bool _isBusy;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "Новый счет";

        public AddAccountViewModel(IDataService dataService, int? accountId = null)
        {
            _dataService = dataService;
            _accountId = accountId;

            if (accountId.HasValue)
            {
                Title = "Редактировать счет";
                IsEditMode = true;
            }
        }

        [RelayCommand]
        private async Task LoadAccountData()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (IsEditMode && _accountId.HasValue)
                {
                    var account = await _dataService.GetAccountByIdAsync(_accountId.Value);
                    if (account != null)
                    {
                        _originalAccount = account;
                        AccountName = account.Name;
                        AccountType = account.Type;
                        Currency = account.Currency;
                        InitialBalance = account.Balance;
                        BankName = account.BankName ?? string.Empty;
                        AccountNumber = account.AccountNumber ?? string.Empty;
                        IsPrimary = account.IsPrimary;
                        IsActive = account.IsActive;
                    }
                }
                else
                {
                    // Установка значений по умолчанию для нового счета
                    AccountName = string.Empty;
                    AccountType = AccountType.Cash;
                    Currency = "RUB";
                    InitialBalance = 0;
                    BankName = string.Empty;
                    AccountNumber = string.Empty;
                    IsPrimary = false;
                    IsActive = true;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAccount()
        {
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите название счета", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode && _originalAccount != null)
                {
                    // Обновление существующего счета
                    _originalAccount.Name = AccountName;
                    _originalAccount.Type = AccountType;
                    _originalAccount.Currency = Currency;
                    _originalAccount.Balance = InitialBalance;
                    _originalAccount.BankName = string.IsNullOrWhiteSpace(BankName) ? null : BankName;
                    _originalAccount.AccountNumber = string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber;
                    _originalAccount.IsPrimary = IsPrimary;
                    _originalAccount.IsActive = IsActive;
                    _originalAccount.LastUpdated = DateTime.Now;

                    await _dataService.UpdateAccountAsync(_originalAccount);
                    await Shell.Current.DisplayAlert("Успех", "Счет обновлен", "OK");
                }
                else
                {
                    // Создание нового счета
                    var newAccount = new Account
                    {
                        Name = AccountName,
                        Type = AccountType,
                        Currency = Currency,
                        Balance = InitialBalance,
                        BankName = string.IsNullOrWhiteSpace(BankName) ? null : BankName,
                        AccountNumber = string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber,
                        IsPrimary = IsPrimary,
                        IsActive = IsActive,
                        LastUpdated = DateTime.Now
                    };

                    await _dataService.AddAccountAsync(newAccount);
                    await Shell.Current.DisplayAlert("Успех", "Счет добавлен", "OK");
                }

                // Возврат на предыдущую страницу
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось сохранить счет: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAccount()
        {
            if (!IsEditMode) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Подтверждение",
                $"Удалить счет '{AccountName}'? Все операции, связанные с этим счетом, останутся в истории, но счет будет недоступен.",
                "Да", "Нет");

            if (confirm)
            {
                IsBusy = true;
                try
                {
                    await _dataService.DeleteAccountAsync(_originalAccount.Id);
                    await Shell.Current.DisplayAlert("Успех", "Счет удален", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить счет: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }
}