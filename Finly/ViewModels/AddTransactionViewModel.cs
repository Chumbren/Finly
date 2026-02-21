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
    public partial class AddTransactionViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private const decimal MAX_AMOUNT = 9999999;

        [ObservableProperty]
        private ObservableCollection<Category> _allCategories = new();

        [ObservableProperty]
        private ObservableCollection<Category> _filteredCategories = new();

        [ObservableProperty]
        private ObservableCollection<Account> _accounts = new();

        [ObservableProperty]
        private Transaction _transaction = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _title = "Новая операция";

        [ObservableProperty]
        private Category _selectedCategory;

        [ObservableProperty]
        private Account _selectedAccount;

        [ObservableProperty]
        private int _transactionTypeIndex;

        // Свойства для валидации
        [ObservableProperty]
        private string _amountBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _descriptionBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _categoryBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _accountBorderColor = "#E0E0E0";

        [ObservableProperty]
        private string _amountValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private string _descriptionValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private string _categoryValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private string _accountValidationStyle = "ErrorFrameStyle";

        [ObservableProperty]
        private bool _showAmountValidation;

        [ObservableProperty]
        private bool _showDescriptionValidation;

        [ObservableProperty]
        private bool _showCategoryValidation;

        [ObservableProperty]
        private bool _showAccountValidation;

        [ObservableProperty]
        private string _amountValidationMessage = "Сумма должна быть от 0.01 до 9 999 999";

        [ObservableProperty]
        private string _descriptionValidationMessage = "Введите описание (от 3 до 100 символов)";

        [ObservableProperty]
        private string _categoryValidationMessage = "Выберите категорию";

        [ObservableProperty]
        private string _accountValidationMessage = "Выберите счет";

        [ObservableProperty]
        private string _amountValidationTextColor = "#D63031";

        [ObservableProperty]
        private string _descriptionValidationTextColor = "#D63031";

        [ObservableProperty]
        private string _categoryValidationTextColor = "#D63031";

        [ObservableProperty]
        private string _accountValidationTextColor = "#D63031";

        [ObservableProperty]
        private bool _isFormValid;

        [ObservableProperty]
        private string _saveButtonColor = "#B2BEC3";

        [ObservableProperty]
        private string _formattedAmount = "0.00";

        public DateTime MinDate => new DateTime(2000, 1, 1);
        public DateTime MaxDate => DateTime.Today;

        public TimeSpan Time
        {
            get => Transaction.Date.TimeOfDay;
            set
            {
                if (Transaction != null)
                {
                    Transaction.Date = Transaction.Date.Date.Add(value);
                    OnPropertyChanged();
                }
            }
        }

        public AddTransactionViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Transaction = new Transaction { Date = DateTime.Now, Type = TransactionType.Expense };
            FormattedAmount = Transaction.Amount.ToString("F2");
        }

        partial void OnFormattedAmountChanged(string value)
        {
            if (decimal.TryParse(value, out decimal amount))
            {
                Transaction.Amount = amount;
            }
            UpdateAmountValidation(value);
            ValidateForm();
        }

        public void UpdateAmountValidation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                AmountBorderColor = "#D63031";
                ShowAmountValidation = true;
                AmountValidationTextColor = "#D63031";
                AmountValidationStyle = "ErrorFrameStyle";
                AmountValidationMessage = "Сумма не может быть пустой";
                return;
            }

            if (!decimal.TryParse(text, out decimal amount))
            {
                AmountBorderColor = "#D63031";
                ShowAmountValidation = true;
                AmountValidationTextColor = "#D63031";
                AmountValidationStyle = "ErrorFrameStyle";
                AmountValidationMessage = "Введите корректное число";
                return;
            }

            if (amount <= 0)
            {
                AmountBorderColor = "#D63031";
                ShowAmountValidation = true;
                AmountValidationTextColor = "#D63031";
                AmountValidationStyle = "ErrorFrameStyle";
                AmountValidationMessage = "Сумма должна быть больше 0";
                return;
            }

            if (amount > MAX_AMOUNT)
            {
                AmountBorderColor = "#D63031";
                ShowAmountValidation = true;
                AmountValidationTextColor = "#D63031";
                AmountValidationStyle = "ErrorFrameStyle";
                AmountValidationMessage = $"Сумма не может превышать {MAX_AMOUNT:N0}";
                return;
            }

            // Все хорошо
            AmountBorderColor = "#2E7D32";
            ShowAmountValidation = true;
            AmountValidationTextColor = "#2E7D32";
            AmountValidationStyle = "SuccessFrameStyle";
            AmountValidationMessage = "✓ Сумма корректна";

            // Форматируем с двумя знаками после запятой
            Transaction.Amount = amount;
            FormattedAmount = amount.ToString("F2");
        }

        public void UpdateDescriptionValidation()
        {
            if (string.IsNullOrWhiteSpace(Transaction.Description))
            {
                DescriptionBorderColor = "#D63031";
                ShowDescriptionValidation = true;
                DescriptionValidationTextColor = "#D63031";
                DescriptionValidationStyle = "ErrorFrameStyle";
                DescriptionValidationMessage = "Введите описание";
                return;
            }

            if (Transaction.Description.Length < 3)
            {
                DescriptionBorderColor = "#D63031";
                ShowDescriptionValidation = true;
                DescriptionValidationTextColor = "#D63031";
                DescriptionValidationStyle = "ErrorFrameStyle";
                DescriptionValidationMessage = "Описание должно быть не менее 3 символов";
                return;
            }

            // Все хорошо
            DescriptionBorderColor = "#2E7D32";
            ShowDescriptionValidation = true;
            DescriptionValidationTextColor = "#2E7D32";
            DescriptionValidationStyle = "SuccessFrameStyle";
            DescriptionValidationMessage = "✓ Описание заполнено";
        }

        public void UpdateCategoryValidation()
        {
            if (SelectedCategory == null)
            {
                CategoryBorderColor = "#D63031";
                ShowCategoryValidation = true;
                CategoryValidationTextColor = "#D63031";
                CategoryValidationStyle = "ErrorFrameStyle";
                CategoryValidationMessage = "Выберите категорию";
                return;
            }

            // Все хорошо
            CategoryBorderColor = "#2E7D32";
            ShowCategoryValidation = true;
            CategoryValidationTextColor = "#2E7D32";
            CategoryValidationStyle = "SuccessFrameStyle";
            CategoryValidationMessage = $"✓ Категория: {SelectedCategory.Name}";
        }

        public void UpdateAccountValidation()
        {
            if (SelectedAccount == null)
            {
                AccountBorderColor = "#D63031";
                ShowAccountValidation = true;
                AccountValidationTextColor = "#D63031";
                AccountValidationStyle = "ErrorFrameStyle";
                AccountValidationMessage = "Выберите счет";
                return;
            }

            // Все хорошо
            AccountBorderColor = "#2E7D32";
            ShowAccountValidation = true;
            AccountValidationTextColor = "#2E7D32";
            AccountValidationStyle = "SuccessFrameStyle";
            AccountValidationMessage = $"✓ Счет: {SelectedAccount.Name}";
        }

        private void ValidateForm()
        {
            bool isAmountValid = Transaction.Amount > 0 && Transaction.Amount <= MAX_AMOUNT;
            bool isDescriptionValid = !string.IsNullOrWhiteSpace(Transaction.Description) && Transaction.Description.Length >= 3;
            bool isCategoryValid = SelectedCategory != null;
            bool isAccountValid = SelectedAccount != null;

            IsFormValid = isAmountValid && isDescriptionValid && isCategoryValid && isAccountValid;
            SaveButtonColor = IsFormValid ? "#00D9A5" : "#B2BEC3";
        }

        // НОВЫЙ МЕТОД: фильтрация категорий по типу операции
        private void FilterCategoriesByType()
        {
            if (AllCategories == null) return;

            var targetType = TransactionTypeIndex == 1 ? CategoryType.Income : CategoryType.Expense;

            FilteredCategories = new ObservableCollection<Category>(
                AllCategories.Where(c => c.Type == targetType)
            );

            Debug.WriteLine($"Фильтрация категорий: тип={targetType}, найдено={FilteredCategories.Count}");

            // Если текущая выбранная категория не подходит под новый тип, сбрасываем выбор
            if (SelectedCategory != null && SelectedCategory.Type != targetType)
            {
                SelectedCategory = FilteredCategories.FirstOrDefault();
                if (SelectedCategory != null)
                {
                    Transaction.CategoryId = SelectedCategory.Id;
                    Debug.WriteLine($"Автоматически выбрана категория: {SelectedCategory.Name}");
                }
                UpdateCategoryValidation();
            }
        }

        [RelayCommand]
        private async Task LoadData(object parameter = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Debug.WriteLine("LoadData: Начало загрузки");

                AllCategories = await _dataService.GetCategoriesAsync();
                Accounts = await _dataService.GetAccountsAsync();

                Debug.WriteLine($"LoadData: Загружено категорий: {AllCategories.Count}, счетов: {Accounts.Count}");

                // Применяем фильтрацию категорий
                FilterCategoriesByType();

                if (!IsEditMode)
                {
                    // Выбираем счет по умолчанию
                    if (Accounts.Any())
                    {
                        var defaultAccount = Accounts.FirstOrDefault(a => a.IsPrimary)
                                          ?? Accounts.FirstOrDefault(a => a.IsActive)
                                          ?? Accounts.First();
                        SelectedAccount = defaultAccount;
                        Transaction.AccountId = defaultAccount.Id;
                        Debug.WriteLine($"Выбран счет: {defaultAccount.Name}");
                        UpdateAccountValidation();
                    }

                    // Выбираем категорию по умолчанию (уже отфильтрованную)
                    if (FilteredCategories.Any() && SelectedCategory == null)
                    {
                        SelectedCategory = FilteredCategories.FirstOrDefault();
                        if (SelectedCategory != null)
                        {
                            Transaction.CategoryId = SelectedCategory.Id;
                            Debug.WriteLine($"Выбрана категория: {SelectedCategory.Name}");
                            UpdateCategoryValidation();
                        }
                    }
                }

                UpdateDescriptionValidation();
                ValidateForm();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка LoadData: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadTransaction(int id)
        {
            IsBusy = true;
            try
            {
                Debug.WriteLine($"LoadTransaction: Загрузка транзакции ID {id}");

                var transaction = await _dataService.GetTransactionByIdAsync(id);
                if (transaction != null)
                {
                    Transaction = new Transaction
                    {
                        Id = transaction.Id,
                        Date = transaction.Date,
                        Amount = transaction.Amount,
                        Description = transaction.Description,
                        CategoryId = transaction.CategoryId,
                        AccountId = transaction.AccountId,
                        Type = transaction.Type,
                        Notes = transaction.Notes,
                        IsRecurring = transaction.IsRecurring,
                        RecurrencePattern = transaction.RecurrencePattern,
                        NextOccurrence = transaction.NextOccurrence
                    };

                    IsEditMode = true;
                    Title = "Редактировать операцию";
                    TransactionTypeIndex = transaction.Type == TransactionType.Income ? 1 : 0;
                    FormattedAmount = transaction.Amount.ToString("F2");

                    await LoadData();

                    // После загрузки данных и фильтрации, выбираем нужную категорию
                    SelectedCategory = FilteredCategories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                    SelectedAccount = Accounts.FirstOrDefault(a => a.Id == transaction.AccountId);

                    UpdateAmountValidation(FormattedAmount);
                    UpdateDescriptionValidation();
                    UpdateCategoryValidation();
                    UpdateAccountValidation();
                    ValidateForm();

                    Debug.WriteLine($"Транзакция загружена: {transaction.Description}, сумма: {transaction.Amount}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка LoadTransaction: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить транзакцию: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveTransaction()
        {
            if (!IsFormValid)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Пожалуйста, заполните все поля корректно", "OK");
                return;
            }

            Transaction.CategoryId = SelectedCategory.Id;
            Transaction.AccountId = SelectedAccount.Id;
            Transaction.Type = TransactionTypeIndex == 1 ? TransactionType.Income : TransactionType.Expense;

            IsBusy = true;
            try
            {
                Debug.WriteLine($"SaveTransaction: Сохранение транзакции. Режим: {(IsEditMode ? "редактирование" : "добавление")}");
                Debug.WriteLine($"Детали: {Transaction.Description}, сумма: {Transaction.Amount}, тип: {Transaction.Type}");

                if (IsEditMode)
                {
                    var result = await _dataService.UpdateTransactionAsync(Transaction);
                    if (result > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Операция обновлена", "OK");
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage
                        {
                            EntityType = "Transaction",
                            EntityId = Transaction.Id,
                            ChangeType = ChangeType.Updated
                        });
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось обновить операцию", "OK");
                        return;
                    }
                }
                else
                {
                    var newId = await _dataService.AddTransactionAsync(Transaction);
                    if (newId > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Операция добавлена", "OK");
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage
                        {
                            EntityType = "Transaction",
                            EntityId = newId,
                            ChangeType = ChangeType.Added
                        });
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось добавить операцию", "OK");
                        return;
                    }
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА сохранения транзакции: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сохранить операцию: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteTransaction()
        {
            if (!IsEditMode) return;

            var confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение",
                "Удалить эту операцию?",
                "Да", "Нет");

            if (confirm)
            {
                IsBusy = true;
                try
                {
                    Debug.WriteLine($"DeleteTransaction: Удаление транзакции ID {Transaction.Id}");

                    var result = await _dataService.DeleteTransactionAsync(Transaction.Id);
                    if (result > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Операция удалена", "OK");

                        WeakReferenceMessenger.Default.Send(new DataChangedMessage
                        {
                            EntityType = "Transaction",
                            EntityId = Transaction.Id,
                            ChangeType = ChangeType.Deleted
                        });

                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось удалить операцию", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ОШИБКА удаления транзакции: {ex}");
                    await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось удалить операцию: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        partial void OnTransactionTypeIndexChanged(int value)
        {
            Transaction.Type = value == 1 ? TransactionType.Income : TransactionType.Expense;
            Debug.WriteLine($"Тип транзакции изменен на: {Transaction.Type}");

            // Фильтруем категории при изменении типа
            FilterCategoriesByType();

            ValidateForm();
        }

        partial void OnSelectedCategoryChanged(Category value)
        {
            if (value != null)
            {
                Transaction.CategoryId = value.Id;
                Debug.WriteLine($"Выбрана категория: {value.Name} (ID: {value.Id})");
                UpdateCategoryValidation();
                ValidateForm();
            }
        }

        partial void OnSelectedAccountChanged(Account value)
        {
            if (value != null)
            {
                Transaction.AccountId = value.Id;
                Debug.WriteLine($"Выбран счет: {value.Name} (ID: {value.Id})");
                UpdateAccountValidation();
                ValidateForm();
            }
        }
    }
}