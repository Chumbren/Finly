using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Finly.Models;
using Finly.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Finly.ViewModels
{
    public partial class AddAccountViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private Account _originalAccount;
        private const decimal MAX_BALANCE = 999999999;

        [ObservableProperty]
        private string _accountName = string.Empty;

        [ObservableProperty]
        private string _selectedAccountType;

        [ObservableProperty]
        private decimal _initialBalance;

        [ObservableProperty]
        private string _formattedBalance = "0.00";

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

        [ObservableProperty]
        private ObservableCollection<string> _accountTypeList = new()
        {
            "Наличные",
            "Банковский счет",
            "Кредитная карта",
            "Инвестиции",
            "Кредит"
        };

        // Свойства для валидации
        [ObservableProperty]
        private string _nameBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _typeBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _balanceBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _nameValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private string _typeValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private string _balanceValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private bool _showNameValidation;

        [ObservableProperty]
        private bool _showTypeValidation;

        [ObservableProperty]
        private bool _showBalanceValidation;

        [ObservableProperty]
        private string _nameValidationMessage = "Введите название счета (от 3 до 50 символов)";

        [ObservableProperty]
        private string _typeValidationMessage = "Выберите тип счета";

        [ObservableProperty]
        private string _balanceValidationMessage = "Баланс должен быть от 0 до 999 999 999";

        [ObservableProperty]
        private string _nameValidationTextColor = "#D63031";

        [ObservableProperty]
        private string _typeValidationTextColor = "#D63031";

        [ObservableProperty]
        private string _balanceValidationTextColor = "#D63031";

        // Фиксированная валюта - всегда рубли
        public string Currency => "RUB";

        public bool IsBankAccount => SelectedAccountType == "Банковский счет" ||
                                      SelectedAccountType == "Кредитная карта";

        public AddAccountViewModel(IDataService dataService)
        {
            _dataService = dataService;
            FormattedBalance = InitialBalance.ToString("F2");
        }

        // При изменении форматированного баланса обновляем числовое значение
        partial void OnFormattedBalanceChanged(string value)
        {
            if (decimal.TryParse(value, out decimal balance))
            {
                InitialBalance = balance;
            }
        }

        // При изменении числового баланса обновляем форматированное значение
        partial void OnInitialBalanceChanged(decimal value)
        {
            FormattedBalance = value.ToString("F2");
            UpdateBalanceValidation(FormattedBalance);
        }

        public void UpdateNameValidation()
        {
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                NameBorderColor = "#D63031";
                ShowNameValidation = true;
                NameValidationTextColor = "#D63031";
                NameValidationStyle = "ErrorFrameStyle";
                NameValidationMessage = "Введите название счета";
            }
            else if (AccountName.Length < 3)
            {
                NameBorderColor = "#D63031";
                ShowNameValidation = true;
                NameValidationTextColor = "#D63031";
                NameValidationStyle = "ErrorFrameStyle";
                NameValidationMessage = "Название должно быть не менее 3 символов";
            }
            else
            {
                // Все хорошо
                NameBorderColor = "#2E7D32";
                ShowNameValidation = true;
                NameValidationTextColor = "#2E7D32";
                NameValidationStyle = "SuccessFrameStyle";
                NameValidationMessage = "✓ Название корректно";
            }
        }

        public void UpdateTypeValidation()
        {
            if (string.IsNullOrWhiteSpace(SelectedAccountType))
            {
                TypeBorderColor = "#D63031";
                ShowTypeValidation = true;
                TypeValidationTextColor = "#D63031";
                TypeValidationStyle = "ErrorFrameStyle";
                TypeValidationMessage = "Выберите тип счета";
            }
            else
            {
                // Все хорошо
                TypeBorderColor = "#2E7D32";
                ShowTypeValidation = true;
                TypeValidationTextColor = "#2E7D32";
                TypeValidationStyle = "SuccessFrameStyle";
                TypeValidationMessage = $"✓ Тип: {SelectedAccountType}";
            }

            OnPropertyChanged(nameof(IsBankAccount));
        }

        public void UpdateBalanceValidation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                BalanceBorderColor = "#D63031";
                ShowBalanceValidation = true;
                BalanceValidationTextColor = "#D63031";
                BalanceValidationStyle = "ErrorFrameStyle";
                BalanceValidationMessage = "Введите баланс";
                return;
            }

            if (!decimal.TryParse(text, out decimal balance))
            {
                BalanceBorderColor = "#D63031";
                ShowBalanceValidation = true;
                BalanceValidationTextColor = "#D63031";
                BalanceValidationStyle = "ErrorFrameStyle";
                BalanceValidationMessage = "Введите корректное число";
                return;
            }

            if (balance < 0)
            {
                BalanceBorderColor = "#D63031";
                ShowBalanceValidation = true;
                BalanceValidationTextColor = "#D63031";
                BalanceValidationStyle = "ErrorFrameStyle";
                BalanceValidationMessage = "Баланс не может быть отрицательным";
                return;
            }

            if (balance > MAX_BALANCE)
            {
                BalanceBorderColor = "#D63031";
                ShowBalanceValidation = true;
                BalanceValidationTextColor = "#D63031";
                BalanceValidationStyle = "ErrorFrameStyle";
                BalanceValidationMessage = $"Баланс не может превышать {MAX_BALANCE:N0}";
                return;
            }

            // Все хорошо
            BalanceBorderColor = "#2E7D32";
            ShowBalanceValidation = true;
            BalanceValidationTextColor = "#2E7D32";
            BalanceValidationStyle = "SuccessFrameStyle";
            BalanceValidationMessage = "✓ Баланс корректен";

            // Важно: обновляем InitialBalance
            InitialBalance = balance;
        }

        [RelayCommand]
        private async Task LoadAccountData(object parameter = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (parameter is int accountId)
                {
                    var account = await _dataService.GetAccountByIdAsync(accountId);
                    if (account != null)
                    {
                        _originalAccount = account;
                        AccountName = account.Name;
                        SelectedAccountType = GetAccountTypeString(account.Type);
                        InitialBalance = account.Balance;
                        FormattedBalance = account.Balance.ToString("F2");
                        BankName = account.BankName ?? string.Empty;
                        AccountNumber = account.AccountNumber ?? string.Empty;
                        IsPrimary = account.IsPrimary;
                        IsActive = account.IsActive;
                        IsEditMode = true;
                        Title = "Редактировать счет";

                        UpdateNameValidation();
                        UpdateTypeValidation();
                        UpdateBalanceValidation(FormattedBalance);
                    }
                }
                else
                {
                    // Новый счет
                    AccountName = string.Empty;
                    SelectedAccountType = null;
                    InitialBalance = 0;
                    FormattedBalance = "0.00";
                    BankName = string.Empty;
                    AccountNumber = string.Empty;
                    IsPrimary = false;
                    IsActive = true;
                    IsEditMode = false;
                    Title = "Новый счет";

                    UpdateNameValidation();
                    UpdateTypeValidation();
                    UpdateBalanceValidation(FormattedBalance);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка LoadAccountData: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetAccountTypeString(AccountType type)
        {
            return type switch
            {
                AccountType.Cash => "Наличные",
                AccountType.BankAccount => "Банковский счет",
                AccountType.CreditCard => "Кредитная карта",
                AccountType.Investment => "Инвестиции",
                AccountType.Loan => "Кредит",
                _ => "Наличные"
            };
        }

        private AccountType GetAccountTypeFromString(string typeString)
        {
            return typeString switch
            {
                "Наличные" => AccountType.Cash,
                "Банковский счет" => AccountType.BankAccount,
                "Кредитная карта" => AccountType.CreditCard,
                "Инвестиции" => AccountType.Investment,
                "Кредит" => AccountType.Loan,
                _ => AccountType.Cash
            };
        }

        [RelayCommand]
        private async Task SaveAccount()
        {
            // Базовая валидация
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Введите название счета", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedAccountType))
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Выберите тип счета", "OK");
                return;
            }

            // Дополнительная проверка баланса
            if (InitialBalance < 0)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Баланс не может быть отрицательным", "OK");
                return;
            }

            if (InitialBalance > MAX_BALANCE)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Баланс не может превышать {MAX_BALANCE:N0}", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var account = new Account
                {
                    Id = _originalAccount?.Id ?? 0,
                    Name = AccountName,
                    Type = GetAccountTypeFromString(SelectedAccountType),
                    Currency = "RUB", // Фиксированная валюта
                    Balance = InitialBalance, // Используем InitialBalance
                    BankName = string.IsNullOrWhiteSpace(BankName) ? null : BankName,
                    AccountNumber = string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber,
                    IsPrimary = IsPrimary,
                    IsActive = IsActive,
                    LastUpdated = DateTime.Now
                };

                Debug.WriteLine($"Сохранение счета: {account.Name}, баланс: {account.Balance}");

                if (IsEditMode)
                {
                    await _dataService.UpdateAccountAsync(account);
                    await Shell.Current.DisplayAlertAsync("Успех", "Счет обновлен", "OK");

                    WeakReferenceMessenger.Default.Send(new DataChangedMessage
                    {
                        EntityType = "Account",
                        EntityId = account.Id,
                        ChangeType = ChangeType.Updated
                    });
                }
                else
                {
                    await _dataService.AddAccountAsync(account);
                    await Shell.Current.DisplayAlertAsync("Успех", "Счет добавлен", "OK");

                    WeakReferenceMessenger.Default.Send(new DataChangedMessage
                    {
                        EntityType = "Account",
                        EntityId = account.Id,
                        ChangeType = ChangeType.Added
                    });
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка SaveAccount: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сохранить счет: {ex.Message}", "OK");
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

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение",
                $"Удалить счет '{AccountName}'?",
                "Да", "Нет");

            if (confirm)
            {
                IsBusy = true;
                try
                {
                    await _dataService.DeleteAccountAsync(_originalAccount.Id);
                    await Shell.Current.DisplayAlertAsync("Успех", "Счет удален", "OK");

                    WeakReferenceMessenger.Default.Send(new DataChangedMessage
                    {
                        EntityType = "Account",
                        EntityId = _originalAccount.Id,
                        ChangeType = ChangeType.Deleted
                    });

                    await Shell.Current.GoToAsync("..");
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
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        partial void OnSelectedAccountTypeChanged(string value)
        {
            UpdateTypeValidation();
            OnPropertyChanged(nameof(IsBankAccount));
        }

        partial void OnAccountNameChanged(string value)
        {
            UpdateNameValidation();
        }
    }
}