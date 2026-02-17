using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Models;
using Finly.Services;
using Finly.Views;
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

        private readonly IDataService _dataService;
        private readonly IPdfExportService _pdfExportService;
        private readonly IServiceProvider _serviceProvider;

        // Также нужно обновить уведомления при изменении CurrentReport
        partial void OnCurrentReportChanged(ReportData value)
        {
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasMonthlyData));
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

        // Доступные типы диаграмм
        [ObservableProperty]
        private ObservableCollection<string> _chartTypes = new() { "Круговая", "Столбчатая", "Линейный график" };


        public ReportsViewModel(
     IDataService dataService,
     IPdfExportService pdfExportService,
     IServiceProvider serviceProvider)  // Добавляем IServiceProvider
        {
            _dataService = dataService;
            _pdfExportService = pdfExportService;
            _serviceProvider = serviceProvider;

            HasData = CurrentReport?.CategoryBreakdown?.Count > 0;

            // Подписка на изменения
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CurrentReport))
                {
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(HasMonthlyData));
                }
            };
        }

        // Команда экспорта с правильным DI
        [RelayCommand]
        private async Task ExportReport()
        {
            if (IsBusy) return;

           
                // Создаем ExportOptionsViewModel через DI
                var exportViewModel = _serviceProvider.GetRequiredService<ExportOptionsViewModel>();

                // Передаем текущий отчет через свойство или метод
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
                Debug.WriteLine($"Получено категорий: {categories?.Count() ?? 0}");

                // Создаем новый объект ReportData
                var newReport = new ReportData();

                newReport.TotalIncome = await _dataService.GetTotalIncomeAsync(ReportStartDate, ReportEndDate);
                newReport.TotalExpenses = await _dataService.GetTotalExpensesAsync(ReportStartDate, ReportEndDate);
                newReport.NetSavings = newReport.TotalIncome - newReport.TotalExpenses;

                Debug.WriteLine($"Доходы: {newReport.TotalIncome}, Расходы: {newReport.TotalExpenses}, Сбережения: {newReport.NetSavings}");

                // Category breakdown
                var categoryBreakdown = new List<CategoryBreakdownItem>();
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
                            categoryBreakdown.Add(new CategoryBreakdownItem
                            {
                                CategoryName = category.Name,
                                CategoryIcon = category.Icon,
                                CategoryColor = string.IsNullOrEmpty(category.Color) ? "#6200EA" : category.Color,
                                Amount = categoryTotal,
                                Percentage = (double)((categoryTotal / totalExpenses) * 100)
                            });
                        }
                    }

                    categoryBreakdown = categoryBreakdown.OrderByDescending(c => c.Amount).ToList();
                }

                newReport.CategoryBreakdown = new ObservableCollection<CategoryBreakdownItem>(categoryBreakdown);
                Debug.WriteLine($"Категорий расходов: {categoryBreakdown.Count}");

                HasData = newReport.CategoryBreakdown.Count > 0;

                // Monthly trends - теперь загружаем всегда, даже если нет расходов
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

                // Присваиваем новый отчет
                CurrentReport = newReport;
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasMonthlyData));
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

        public void DrawChart(SKCanvas canvas, int width, int height)
        {
            canvas.Clear(SKColors.White);

            if (ChartType == "Линейный график")
            {
                DrawLineChart(canvas, width, height);
                return;
            }

            if (CurrentReport?.CategoryBreakdown == null || CurrentReport.CategoryBreakdown.Count == 0)
            {
                DrawNoDataMessage(canvas, width, height);
                return;
            }

            if (ChartType == "Круговая")
                DrawPieChart(canvas, width, height);
            else if (ChartType == "Столбчатая")
                DrawBarChart(canvas, width, height);
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

        private void DrawPieChart(SKCanvas canvas, int width, int height)
        {
            var centerX = width / 2f;
            var centerY = height / 2f;
            var radius = Math.Min(width, height) / 2.5f; // Увеличил радиус

            float startAngle = -90;

            // Нормализация процентов
            var totalPercentage = CurrentReport.CategoryBreakdown.Sum(c => c.Percentage);
            if (Math.Abs(totalPercentage - 100) > 0.01)
            {
                var factor = 100 / totalPercentage;
                foreach (var item in CurrentReport.CategoryBreakdown)
                {
                    item.Percentage *= factor;
                }
            }

            // Рисуем сегменты
            foreach (var item in CurrentReport.CategoryBreakdown)
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
            DrawLegend(canvas, width, height);
        }

        private void DrawBarChart(SKCanvas canvas, int width, int height)
        {
            var margin = 50f;
            var chartWidth = width - 2 * margin;
            var chartHeight = height - 2 * margin;
            var bottomY = height - margin;

            var items = CurrentReport.CategoryBreakdown.Take(8).ToList(); // Показываем до 8 категорий
            var barWidth = (chartWidth - (items.Count - 1) * 15) / items.Count; // Автоматический расчет ширины
            barWidth = Math.Min(barWidth, 60f); // Максимальная ширина 60px

            var maxAmount = (float)CurrentReport.CategoryBreakdown.Max(c => (float)c.Amount);
            var scale = (float)(chartHeight - 30) / maxAmount;

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
                // Вертикальная ось
                canvas.DrawLine(margin - 5, margin, margin - 5, bottomY, axisPaint);
                // Горизонтальная ось
                canvas.DrawLine(margin - 5, bottomY, width - margin + 5, bottomY, axisPaint);
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

                // Столбец
                canvas.DrawRect(x, bottomY - barHeight, barWidth, barHeight, fillPaint);

                // Обводка
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
                    TextSize = 12,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };
                canvas.DrawText($"{item.Amount:C0}", x + barWidth / 2, bottomY - barHeight - 5, valuePaint);

                // Название категории
                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 11,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };

                var categoryName = item.CategoryName.Length > 8
                    ? item.CategoryName.Substring(0, 8) + "..."
                    : item.CategoryName;

                canvas.DrawText(categoryName, x + barWidth / 2, bottomY + 15, textPaint);

                x += barWidth + 15;
            }
        }

        private void DrawLineChart(SKCanvas canvas, int width, int height)
        {
            if (CurrentReport?.MonthlyTrends == null || CurrentReport.MonthlyTrends.Count < 2)
            {
                DrawNoDataMessage(canvas, width, height);
                return;
            }

            var margin = 80f;
            var chartWidth = width - 2 * margin;
            var chartHeight = height - 2 * margin;
            var bottomY = height - margin;
            var leftX = margin;

            var trends = CurrentReport.MonthlyTrends.ToList();

            // 🔥 ФИКС: Нормализуем данные для отображения
            var normalizedTrends = NormalizeTrends(trends);

            // Находим максимальное значение после нормализации (макс будет 100%)
            var maxValue = 100f; // После нормализации максимум 100%

            var scale = chartHeight / maxValue;
            var stepX = trends.Count > 1 ? chartWidth / (trends.Count - 1) : chartWidth;

            // Рисуем сетку
            DrawChartGrid(canvas, leftX, bottomY, chartWidth, chartHeight, maxValue, margin, width);

            // Рисуем оси
            using (var axisPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawLine(leftX, margin - 10, leftX, bottomY + 10, axisPaint);
                canvas.DrawLine(leftX - 10, bottomY, width - margin + 10, bottomY, axisPaint);
            }

            // Собираем точки для доходов и расходов (используем нормализованные значения)
            var incomePoints = new List<SKPoint>();
            var expensePoints = new List<SKPoint>();

            for (int i = 0; i < trends.Count; i++)
            {
                var x = leftX + i * stepX;

                incomePoints.Add(new SKPoint(x, bottomY - normalizedTrends[i].NormalizedIncome * scale));
                expensePoints.Add(new SKPoint(x, bottomY - normalizedTrends[i].NormalizedExpenses * scale));
            }

            // Рисуем линии и точки
            DrawLineSeriesWithPoints(canvas, incomePoints, SKColors.Green, "Доходы");
            DrawLineSeriesWithPoints(canvas, expensePoints, SKColors.Red, "Расходы");

            // Подписи
            using (var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 11,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            })
            {
                for (int i = 0; i < trends.Count; i++)
                {
                    var x = leftX + i * stepX;

                    // Название месяца
                    canvas.DrawText(trends[i].MonthName, x, bottomY + 25, textPaint);

                    // 🔥 ФИКС: Показываем реальные значения в более читаемом формате
                    using (var valuePaint = new SKPaint
                    {
                        Color = SKColors.DarkGray,
                        TextSize = 9,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Center
                    })
                    {
                        // Форматируем огромные числа
                        var incomeStr = FormatLargeNumber(trends[i].Income);
                        var expenseStr = FormatLargeNumber(trends[i].Expenses);

                        if (trends[i].Income > 0)
                        {
                            canvas.DrawText($"Д:{incomeStr}", x, incomePoints[i].Y - 15, valuePaint);
                        }

                        if (trends[i].Expenses > 0)
                        {
                            canvas.DrawText($"Р:{expenseStr}", x, expensePoints[i].Y + 20, valuePaint);
                        }
                    }
                }
            }

            // Легенда с пояснением
            using (var infoPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 10,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left
            })
            {
                canvas.DrawText("* График показывает относительные изменения (в % от максимума)",
                    margin, margin - 30, infoPaint);
            }

            DrawChartLegend(canvas, width, margin);
        }


        /// <summary>
/// Нормализует значения для отображения в процентах от максимума
/// </summary>
private List<NormalizedTrend> NormalizeTrends(List<MonthlyTrendItem> trends)
        {
            var maxIncome = trends.Max(t => t.Income);
            var maxExpenses = trends.Max(t => t.Expenses);
            var globalMax = Math.Max(maxIncome, maxExpenses);

            if (globalMax == 0) globalMax = 1; // Защита от деления на ноль

            return trends.Select(t => new NormalizedTrend
            {
                MonthName = t.MonthName,
                Income = t.Income,
                Expenses = t.Expenses,
                NormalizedIncome = (float)(t.Income / globalMax * 100),
                NormalizedExpenses = (float)(t.Expenses / globalMax * 100)
            }).ToList();
        }

        /// <summary>
        /// Форматирует огромные числа в читаемый вид
        /// </summary>
        private string FormatLargeNumber(decimal number)
        {
            if (number >= 1_000_000_000_000) // Триллионы
                return $"{number / 1_000_000_000_000m:F1}трлн";
            if (number >= 1_000_000_000) // Миллиарды
                return $"{number / 1_000_000_000m:F1}млрд";
            if (number >= 1_000_000) // Миллионы
                return $"{number / 1_000_000m:F1}млн";
            if (number >= 1_000) // Тысячи
                return $"{number / 1_000m:F1}тыс";

            return $"{number:F0}";
        }

        private void DrawChartGrid(SKCanvas canvas, float leftX, float bottomY,
    float chartWidth, float chartHeight, float maxValue, float margin, float width)
        {
            using (var gridPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.LightGray.WithAlpha(0x80),
                StrokeWidth = 1,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            })
            {
                // Горизонтальные линии сетки (5 линий)
                for (int i = 0; i <= 5; i++)
                {
                    var y = bottomY - (i * chartHeight / 5);
                    canvas.DrawLine(leftX, y, width - margin, y, gridPaint);

                    // 🔥 ИСПРАВЛЕНО: убрал форматирование валюты для процентов
                    using var textPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        TextSize = 10,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Right
                    };
                    var percentage = (maxValue / 5) * i;
                    canvas.DrawText($"{percentage:F0}%", leftX - 10, y + 3, textPaint);
                }
            }
        }
        private void DrawLineSeriesWithPoints(SKCanvas canvas, List<SKPoint> points, SKColor color, string label)
        {
            if (points.Count < 2) return;

            // Рисуем линию
            using (var linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color,
                StrokeWidth = 3,
                IsAntialias = true
            })
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    canvas.DrawLine(points[i], points[i + 1], linePaint);
                }
            }

            // Рисуем точки
            using (var pointPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = color,
                IsAntialias = true
            })
            {
                foreach (var point in points)
                {
                    // Пропускаем точки с отрицательными координатами (если значение 0)
                    if (point.Y <= canvas.LocalClipBounds.Top + 10) continue;

                    canvas.DrawCircle(point, 6, pointPaint);

                    // Белая обводка для точек
                    using var strokePaint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.White,
                        StrokeWidth = 2,
                        IsAntialias = true
                    };
                    canvas.DrawCircle(point, 6, strokePaint);
                }
            }
        }
       

      

        private void DrawLegend(SKCanvas canvas, int width, int height)
        {
            var items = CurrentReport.CategoryBreakdown.Take(5).ToList();
            var legendX = 20f;
            var legendY = height - 100f;
            var spacing = 25f;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true
            };

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var color = SKColor.Parse(item.CategoryColor);

                using var rectPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color
                };

                canvas.DrawRect(legendX, legendY + i * spacing, 15, 15, rectPaint);
                canvas.DrawText($"{item.CategoryName} ({item.Percentage:F1}%)",
                    legendX + 20, legendY + i * spacing + 12, textPaint);
            }
        }

        private static void DrawChartLegend(SKCanvas canvas, int width, float margin)
        {
            var legendX = width - 150;
            var legendY = margin + 20;
            var spacing = 25f;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true
            };

            // Доходы
            using (var incomePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Green })
            {
                canvas.DrawRect(legendX, legendY, 15, 15, incomePaint);
            }
            canvas.DrawText("Доходы", legendX + 20, legendY + 12, textPaint);

            // Расходы
            using (var expensePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Red })
            {
                canvas.DrawRect(legendX, legendY + spacing, 15, 15, expensePaint);
            }
            canvas.DrawText("Расходы", legendX + 20, legendY + spacing + 12, textPaint);
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

    // Остальные классы остаются без изменений
    public partial class ReportData : ObservableObject
    {
        [ObservableProperty]
        private decimal _totalIncome;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _netSavings;

        [ObservableProperty]
        private ObservableCollection<CategoryBreakdownItem> _categoryBreakdown = new();

        [ObservableProperty]
        private ObservableCollection<MonthlyTrendItem> _monthlyTrends = new();
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
    }
    public class NormalizedTrend
    {
        public string MonthName { get; set; }
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public float NormalizedIncome { get; set; }
        public float NormalizedExpenses { get; set; }
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