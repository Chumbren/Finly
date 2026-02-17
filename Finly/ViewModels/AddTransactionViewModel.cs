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

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

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

        public AddTransactionViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Transaction = new Transaction { Date = DateTime.Now, Type = TransactionType.Expense };
        }

        [RelayCommand]
        private async Task LoadData(object parameter = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Debug.WriteLine("LoadData: Начало загрузки");

                Categories = await _dataService.GetCategoriesAsync();
                Accounts = await _dataService.GetAccountsAsync();

                Debug.WriteLine($"LoadData: Загружено категорий: {Categories.Count}, счетов: {Accounts.Count}");

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
                    }

                    // Выбираем категорию по умолчанию в зависимости от типа
                    if (Categories.Any())
                    {
                        var targetType = TransactionTypeIndex == 1 ? CategoryType.Income : CategoryType.Expense;
                        var defaultCategory = Categories.FirstOrDefault(c => c.Type == targetType)
                                           ?? Categories.First();
                        SelectedCategory = defaultCategory;
                        Transaction.CategoryId = defaultCategory.Id;
                        Debug.WriteLine($"Выбрана категория: {defaultCategory.Name}");
                    }
                }
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

                    await LoadData();

                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                    SelectedAccount = Accounts.FirstOrDefault(a => a.Id == transaction.AccountId);

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
            if (Transaction.Amount <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Сумма должна быть больше 0", "OK");
                return;
            }

            if (SelectedCategory == null)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Выберите категорию", "OK");
                return;
            }

            if (SelectedAccount == null)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Выберите счет", "OK");
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
                        // Отправляем сообщение об обновлении
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
                        // Отправляем сообщение о добавлении
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

                        // Отправляем сообщение об удалении
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

        partial void OnTransactionTypeIndexChanged(int value)
        {
            Transaction.Type = value == 1 ? TransactionType.Income : TransactionType.Expense;
            Debug.WriteLine($"Тип транзакции изменен на: {Transaction.Type}");

            if (Categories.Any())
            {
                var targetCategoryType = value == 1 ? CategoryType.Income : CategoryType.Expense;
                var defaultCategory = Categories.FirstOrDefault(c => c.Type == targetCategoryType);
                if (defaultCategory != null)
                {
                    SelectedCategory = defaultCategory;
                    Transaction.CategoryId = defaultCategory.Id;
                    Debug.WriteLine($"Категория изменена на: {defaultCategory.Name}");
                }
            }
        }

        partial void OnSelectedCategoryChanged(Category value)
        {
            if (value != null)
            {
                Transaction.CategoryId = value.Id;
                Debug.WriteLine($"Выбрана категория: {value.Name} (ID: {value.Id})");
            }
        }

        partial void OnSelectedAccountChanged(Account value)
        {
            if (value != null)
            {
                Transaction.AccountId = value.Id;
                Debug.WriteLine($"Выбран счет: {value.Name} (ID: {value.Id})");
            }
        }
    }
}