using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Finly.Models;
using Finly.Services;
using Finly.Views;
using Finly.Views.ModelWindows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Finly.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        public bool HasMonthlyData => CurrentReport?.MonthlyTrends?.Count > 0;
        public bool HasTransactions => CurrentReport?.Transactions?.Count > 0;
        public bool HasIncomeData => CurrentReport?.IncomeBreakdown?.Count > 0;
        public bool HasExpenseData => CurrentReport?.ExpenseBreakdown?.Count > 0;

        private readonly IDataService _dataService;
        private readonly IPdfExportService _pdfExportService;
        private readonly IServiceProvider _serviceProvider;

        partial void OnCurrentReportChanged(ReportData value)
        {
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasMonthlyData));
            OnPropertyChanged(nameof(HasTransactions));
            OnPropertyChanged(nameof(HasIncomeData));
            OnPropertyChanged(nameof(HasExpenseData));
        }

        [ObservableProperty]
        private ReportData _currentReport = new();

        [ObservableProperty]
        private DateTime _reportStartDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _reportEndDate = DateTime.Today;

        [ObservableProperty]
        private string _chartType = "Круговая";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private ObservableCollection<string> _chartTypes = new() { "Круговая", "Столбчатая" };

        // Поиск по операциям
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<TransactionDisplayItem> _filteredTransactions = new();

        // Для отображения в каком формате показывать числа
        [ObservableProperty]
        private string _amountDisplayMode = "Обычный"; // "Обычный", "Тысячи", "Миллионы", "Миллиарды"

        public ReportsViewModel(
            IDataService dataService,
            IPdfExportService pdfExportService,
            IServiceProvider serviceProvider)
        {
            _dataService = dataService;
            _pdfExportService = pdfExportService;
            _serviceProvider = serviceProvider;

            // ИСПРАВЛЕНО: заменяем CategoryBreakdown на проверку IncomeBreakdown и ExpenseBreakdown
            HasData = (CurrentReport?.IncomeBreakdown?.Count > 0) || (CurrentReport?.ExpenseBreakdown?.Count > 0);

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CurrentReport))
                {
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(HasMonthlyData));
                    OnPropertyChanged(nameof(HasTransactions));
                    OnPropertyChanged(nameof(HasIncomeData));
                    OnPropertyChanged(nameof(HasExpenseData));

                    // Обновляем отфильтрованные транзакции
                    FilterTransactions();

                    // Определяем режим отображения чисел
                    DetermineAmountDisplayMode();
                }
            };
        }

        // Определяем режим отображения чисел на основе максимальной суммы
        private void DetermineAmountDisplayMode()
        {
            if (CurrentReport == null) return;

            var maxAmount = Math.Max(
                CurrentReport.TotalIncome,
                CurrentReport.TotalExpenses
            );

            if (maxAmount >= 1_000_000_000)
                AmountDisplayMode = "Миллиарды";
            else if (maxAmount >= 1_000_000)
                AmountDisplayMode = "Миллионы";
            else if (maxAmount >= 1_000)
                AmountDisplayMode = "Тысячи";
            else
                AmountDisplayMode = "Обычный";
        }

        // Форматирование больших чисел
        public string FormatLargeAmount(decimal amount)
        {
            if (AmountDisplayMode == "Миллиарды")
                return $"{amount / 1_000_000_000m:F2} млрд ₽";
            else if (AmountDisplayMode == "Миллионы")
                return $"{amount / 1_000_000m:F2} млн ₽";
            else if (AmountDisplayMode == "Тысячи")
                return $"{amount / 1_000m:F2} тыс ₽";
            else
                return amount.ToString("C0");
        }

        // Команда экспорта с правильным DI
        [RelayCommand]
        private async Task ExportReport()
        {
            if (IsBusy) return;

            var exportViewModel = _serviceProvider.GetRequiredService<ExportOptionsViewModel>();
            exportViewModel.Initialize(this);

            var exportPage = new ExportOptionsPage(exportViewModel);
            await Shell.Current.Navigation.PushModalAsync(exportPage);
        }

        [RelayCommand]
        private async Task GenerateReport()
        {
            if (IsBusy) return;
            IsBusy = true;
            HasData = false;

            try
            {
                Debug.WriteLine($"Генерация отчета с {ReportStartDate:d} по {ReportEndDate:d}");

                var transactions = await _dataService.GetTransactionsAsync(ReportStartDate, ReportEndDate);
                Debug.WriteLine($"Получено транзакций: {transactions?.Count() ?? 0}");

                var categories = await _dataService.GetCategoriesAsync();
                var accounts = await _dataService.GetAccountsAsync();
                Debug.WriteLine($"Получено категорий: {categories?.Count() ?? 0}, счетов: {accounts?.Count() ?? 0}");

                // Создаем новый объект ReportData
                var newReport = new ReportData();

                newReport.TotalIncome = await _dataService.GetTotalIncomeAsync(ReportStartDate, ReportEndDate);
                newReport.TotalExpenses = await _dataService.GetTotalExpensesAsync(ReportStartDate, ReportEndDate);
                newReport.NetSavings = newReport.TotalIncome - newReport.TotalExpenses;

                Debug.WriteLine($"Доходы: {newReport.TotalIncome}, Расходы: {newReport.TotalExpenses}, Сбережения: {newReport.NetSavings}");

                // Детализация доходов по категориям
                var incomeBreakdown = new List<CategoryBreakdownItem>();
                var totalIncome = newReport.TotalIncome;

                if (totalIncome > 0)
                {
                    var incomeCategories = categories.Where(c => c.Type == CategoryType.Income).ToList();

                    foreach (var category in incomeCategories)
                    {
                        var categoryTotal = transactions
                            .Where(t => t.CategoryId == category.Id && t.Type == TransactionType.Income)
                            .Sum(t => t.Amount);

                        if (categoryTotal > 0)
                        {
                            incomeBreakdown.Add(new CategoryBreakdownItem
                            {
                                CategoryName = category.Name,
                                CategoryIcon = category.Icon,
                                CategoryColor = string.IsNullOrEmpty(category.Color) ? "#4CAF50" : category.Color,
                                Amount = categoryTotal,
                                Percentage = (double)((categoryTotal / totalIncome) * 100)
                            });
                        }
                    }

                    incomeBreakdown = incomeBreakdown.OrderByDescending(c => c.Amount).ToList();
                }

                newReport.IncomeBreakdown = new ObservableCollection<CategoryBreakdownItem>(incomeBreakdown);
                Debug.WriteLine($"Категорий доходов: {incomeBreakdown.Count}");

                // Детализация расходов по категориям
                var expenseBreakdown = new List<CategoryBreakdownItem>();
                var totalExpenses = newReport.TotalExpenses;

                if (totalExpenses > 0)
                {
                    var expenseCategories = categories.Where(c => c.Type == CategoryType.Expense).ToList();

                    foreach (var category in expenseCategories)
                    {
                        var categoryTotal = transactions
                            .Where(t => t.CategoryId == category.Id && t.Type == TransactionType.Expense)
                            .Sum(t => t.Amount);

                        if (categoryTotal > 0)
                        {
                            expenseBreakdown.Add(new CategoryBreakdownItem
                            {
                                CategoryName = category.Name,
                                CategoryIcon = category.Icon,
                                CategoryColor = string.IsNullOrEmpty(category.Color) ? "#F44336" : category.Color,
                                Amount = categoryTotal,
                                Percentage = (double)((categoryTotal / totalExpenses) * 100)
                            });
                        }
                    }

                    expenseBreakdown = expenseBreakdown.OrderByDescending(c => c.Amount).ToList();
                }

                newReport.ExpenseBreakdown = new ObservableCollection<CategoryBreakdownItem>(expenseBreakdown);
                Debug.WriteLine($"Категорий расходов: {expenseBreakdown.Count}");

                HasData = expenseBreakdown.Count > 0 || incomeBreakdown.Count > 0;

                // Monthly trends
                var monthlyTrends = new List<MonthlyTrendItem>();
                var currentDate = ReportStartDate;

                while (currentDate <= ReportEndDate)
                {
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    if (monthEnd > ReportEndDate)
                        monthEnd = ReportEndDate;

                    var income = await _dataService.GetTotalIncomeAsync(monthStart, monthEnd);
                    var expenses = await _dataService.GetTotalExpensesAsync(monthStart, monthEnd);

                    monthlyTrends.Add(new MonthlyTrendItem
                    {
                        MonthName = monthStart.ToString("MMM yyyy"),
                        Income = income,
                        Expenses = expenses,
                        Savings = income - expenses
                    });

                    currentDate = currentDate.AddMonths(1);
                }

                newReport.MonthlyTrends = new ObservableCollection<MonthlyTrendItem>(monthlyTrends);

                // Загружаем все транзакции за период с деталями
                var transactionItems = new ObservableCollection<TransactionDisplayItem>();

                foreach (var transaction in transactions.OrderByDescending(t => t.Date))
                {
                    var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryId)
                                ?? new Category { Name = "Без категории", Icon = "❓", Color = "#9E9E9E" };

                    var account = accounts.FirstOrDefault(a => a.Id == transaction.AccountId)
                               ?? new Account { Name = "Неизвестный счет" };

                    transactionItems.Add(new TransactionDisplayItem
                    {
                        Transaction = transaction,
                        Category = category,
                        Account = account
                    });
                }

                newReport.Transactions = transactionItems;
                Debug.WriteLine($"Загружено транзакций для отображения: {transactionItems.Count}");

                // Присваиваем новый отчет
                CurrentReport = newReport;

                // Фильтруем транзакции
                FilterTransactions();

                // Определяем режим отображения чисел
                DetermineAmountDisplayMode();

                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasMonthlyData));
                OnPropertyChanged(nameof(HasTransactions));
                OnPropertyChanged(nameof(HasIncomeData));
                OnPropertyChanged(nameof(HasExpenseData));
                Debug.WriteLine("Отчет успешно сгенерирован");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при генерации отчета: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сгенерировать отчет: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Фильтрация транзакций по поисковому запросу
        private void FilterTransactions()
        {
            if (CurrentReport?.Transactions == null)
            {
                FilteredTransactions.Clear();
                return;
            }

            var query = SearchQuery?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredTransactions = new ObservableCollection<TransactionDisplayItem>(CurrentReport.Transactions);
            }
            else
            {
                var filtered = CurrentReport.Transactions
                    .Where(t =>
                        t.Description?.ToLower().Contains(query) == true ||
                        t.CategoryName?.ToLower().Contains(query) == true ||
                        t.AccountName?.ToLower().Contains(query) == true ||
                        t.Amount.ToString().Contains(query))
                    .ToList();

                FilteredTransactions = new ObservableCollection<TransactionDisplayItem>(filtered);
            }

            OnPropertyChanged(nameof(FilteredTransactions));
        }

        partial void OnSearchQueryChanged(string value)
        {
            FilterTransactions();
        }

        public void DrawChart(SKCanvas canvas, int width, int height)
        {
            canvas.Clear(SKColors.White);

            // Определяем, какие данные показывать в зависимости от выбранного типа диаграммы
            var hasIncome = CurrentReport?.IncomeBreakdown?.Count > 0;
            var hasExpense = CurrentReport?.ExpenseBreakdown?.Count > 0;

            if (!hasIncome && !hasExpense)
            {
                DrawNoDataMessage(canvas, width, height);
                return;
            }

            if (ChartType == "Круговая")
            {
                // Для круговой диаграммы показываем только расходы (как более показательные)
                if (hasExpense)
                    DrawPieChart(canvas, width, height, CurrentReport.ExpenseBreakdown);
                else if (hasIncome)
                    DrawPieChart(canvas, width, height, CurrentReport.IncomeBreakdown);
            }
            else if (ChartType == "Столбчатая")
            {
                // Для столбчатой показываем сравнение доходов и расходов по категориям
                DrawComparisonBarChart(canvas, width, height);
            }
        }

        private void DrawNoDataMessage(SKCanvas canvas, int width, int height)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 20,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText("Нет данных для отображения", width / 2, height / 2, paint);
        }

        private void DrawPieChart(SKCanvas canvas, int width, int height, ObservableCollection<CategoryBreakdownItem> items)
        {
            var centerX = width / 2f;
            var centerY = height / 2f;
            var radius = Math.Min(width, height) / 2.5f;

            float startAngle = -90;

            // Нормализация процентов
            var totalPercentage = items.Sum(c => c.Percentage);
            if (Math.Abs(totalPercentage - 100) > 0.01)
            {
                var factor = 100 / totalPercentage;
                foreach (var item in items)
                {
                    item.Percentage *= factor;
                }
            }

            // Рисуем сегменты
            foreach (var item in items)
            {
                var sweepAngle = (float)(item.Percentage * 3.6); // 360/100 = 3.6
                var color = SKColor.Parse(item.CategoryColor);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color,
                    IsAntialias = true
                };

                canvas.DrawArc(new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius),
                    startAngle, sweepAngle, true, paint);

                startAngle += sweepAngle;
            }

            // Рисуем легенду
            DrawLegend(canvas, width, height, items);
        }

        private void DrawComparisonBarChart(SKCanvas canvas, int width, int height)
        {
            var margin = 60f;
            var chartWidth = width - 2 * margin;
            var chartHeight = height - 2 * margin;
            var bottomY = height - margin;

            // Объединяем все категории (доходы и расходы) для отображения
            var allItems = new List<CategoryBreakdownItem>();

            if (CurrentReport.IncomeBreakdown != null)
                allItems.AddRange(CurrentReport.IncomeBreakdown.Select(i =>
                    new CategoryBreakdownItem
                    {
                        CategoryName = i.CategoryName + " (Д)",
                        CategoryIcon = i.CategoryIcon,
                        CategoryColor = i.CategoryColor,
                        Amount = i.Amount,
                        Percentage = i.Percentage,
                        IsIncome = true
                    }));

            if (CurrentReport.ExpenseBreakdown != null)
                allItems.AddRange(CurrentReport.ExpenseBreakdown.Select(e =>
                    new CategoryBreakdownItem
                    {
                        CategoryName = e.CategoryName + " (Р)",
                        CategoryIcon = e.CategoryIcon,
                        CategoryColor = e.CategoryColor,
                        Amount = e.Amount,
                        Percentage = e.Percentage,
                        IsIncome = false
                    }));

            var items = allItems.OrderByDescending(i => i.Amount).Take(8).ToList();

            if (items.Count == 0) return;

            var barWidth = (chartWidth - (items.Count - 1) * 15) / items.Count;
            barWidth = Math.Min(barWidth, 70f);

            var maxAmount = (float)items.Max(i => (float)i.Amount);
            var scale = (float)(chartHeight - 50) / maxAmount;

            var x = margin;

            // Рисуем оси
            using (var axisPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawLine(margin - 5, margin, margin - 5, bottomY, axisPaint);
                canvas.DrawLine(margin - 5, bottomY, width - margin + 5, bottomY, axisPaint);
            }

            // Рисуем сетку по горизонтали
            using (var gridPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.LightGray.WithAlpha(0x80),
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            })
            {
                for (int i = 0; i <= 5; i++)
                {
                    var y = bottomY - (i * chartHeight / 5);
                    canvas.DrawLine(margin - 5, y, width - margin + 5, y, gridPaint);

                    using var textPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        TextSize = 10,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Right
                    };
                    var value = maxAmount / 5 * i;
                    canvas.DrawText(FormatShortAmount((decimal)value), margin - 15, y + 3, textPaint);
                }
            }

            // Рисуем столбцы
            foreach (var item in items)
            {
                var barHeight = (float)item.Amount * scale;
                var color = SKColor.Parse(item.CategoryColor);

                using var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color,
                    IsAntialias = true
                };

                canvas.DrawRect(x, bottomY - barHeight, barWidth, barHeight, fillPaint);

                using var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black,
                    StrokeWidth = 1,
                    IsAntialias = true
                };
                canvas.DrawRect(x, bottomY - barHeight, barWidth, barHeight, strokePaint);

                // Значение сверху
                using var valuePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 11,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };
                canvas.DrawText(FormatShortAmount(item.Amount), x + barWidth / 2, bottomY - barHeight - 5, valuePaint);

                // Название категории
                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 10,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };

                var categoryName = item.CategoryName.Length > 10
                    ? item.CategoryName.Substring(0, 10) + ".."
                    : item.CategoryName;

                canvas.DrawText(categoryName, x + barWidth / 2, bottomY + 20, textPaint);

                // Иконка типа (⬆ для дохода, ⬇ для расхода)
                using var iconPaint = new SKPaint
                {
                    Color = item.IsIncome ? SKColors.Green : SKColors.Red,
                    TextSize = 12,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };
                canvas.DrawText(item.IsIncome ? "⬆" : "⬇", x + barWidth / 2, bottomY + 35, iconPaint);

                x += barWidth + 15;
            }
        }

        private string FormatShortAmount(decimal amount)
        {
            if (amount >= 1_000_000_000)
                return $"{amount / 1_000_000_000m:F1} млрд";
            if (amount >= 1_000_000)
                return $"{amount / 1_000_000m:F1} млн";
            if (amount >= 1_000)
                return $"{amount / 1_000m:F1} тыс";
            return $"{amount:F0}";
        }

        private void DrawLegend(SKCanvas canvas, int width, int height, ObservableCollection<CategoryBreakdownItem> items)
        {
            var legendX = 20f;
            var legendY = height - 120f;
            var spacing = 25f;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 11,
                IsAntialias = true
            };

            for (int i = 0; i < Math.Min(items.Count, 6); i++)
            {
                var item = items[i];
                var color = SKColor.Parse(item.CategoryColor);

                using var rectPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color
                };

                canvas.DrawRect(legendX, legendY + i * spacing, 15, 15, rectPaint);

                var shortName = item.CategoryName.Length > 15
                    ? item.CategoryName.Substring(0, 15) + ".."
                    : item.CategoryName;

                canvas.DrawText($"{shortName} ({item.Percentage:F1}%)",
                    legendX + 20, legendY + i * spacing + 12, textPaint);
            }
        }

        partial void OnReportStartDateChanged(DateTime value)
        {
            if (value > ReportEndDate)
                ReportEndDate = value;
            _ = GenerateReport();
        }

        partial void OnReportEndDateChanged(DateTime value)
        {
            if (value < ReportStartDate)
                ReportStartDate = value;
            _ = GenerateReport();
        }
    }

    public partial class ReportData : ObservableObject
    {
        [ObservableProperty]
        private decimal _totalIncome;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _netSavings;

        [ObservableProperty]
        private ObservableCollection<CategoryBreakdownItem> _incomeBreakdown = new();

        [ObservableProperty]
        private ObservableCollection<CategoryBreakdownItem> _expenseBreakdown = new();

        [ObservableProperty]
        private ObservableCollection<MonthlyTrendItem> _monthlyTrends = new();

        [ObservableProperty]
        private ObservableCollection<TransactionDisplayItem> _transactions = new();
    }

    public partial class CategoryBreakdownItem : ObservableObject
    {
        [ObservableProperty]
        private string _categoryName = string.Empty;

        [ObservableProperty]
        private string _categoryIcon = string.Empty;

        [ObservableProperty]
        private string _categoryColor = string.Empty;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private double _percentage;

        [ObservableProperty]
        private bool _isIncome;
    }

    public partial class MonthlyTrendItem : ObservableObject
    {
        [ObservableProperty]
        private string _monthName = string.Empty;

        [ObservableProperty]
        private decimal _income;

        [ObservableProperty]
        private decimal _expenses;

        [ObservableProperty]
        private decimal _savings;
    }
}