using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Finly.Models;
using Finly.ViewModels;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Finly.Services
{
    public class PdfExportService : IPdfExportService
    {
        private const int PageWidth = 842;
        private const int PageHeight = 1190;
        private const int Margin = 50;

        private readonly IFileSaver _fileSaver;

        public PdfExportService(IFileSaver fileSaver)
        {
            _fileSaver = fileSaver;
        }

        public async Task<bool> ExportReportToPdfAsync(ReportData report, DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                var fileName = $"Financial_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";

                // Создаем PDF в памяти
                using var memoryStream = new MemoryStream();
                using (var pdf = SKDocument.CreatePdf(memoryStream))
                {
                    DrawCoverPage(pdf, report, startDate, endDate);

                    if (reportType == "Текущий отчет" || reportType == "Полный отчет за период")
                    {
                        if (report.IncomeBreakdown?.Count > 0 || report.ExpenseBreakdown?.Count > 0)
                        {
                            DrawChartsPage(pdf, report);
                        }

                        if (report.ExpenseBreakdown?.Count > 0)
                            DrawExpenseCategoriesPage(pdf, report);

                        if (report.IncomeBreakdown?.Count > 0)
                            DrawIncomeCategoriesPage(pdf, report);

                        // ВСЕГДА добавляем страницу с транзакциями для полного отчета
                        if (report.Transactions?.Count > 0)
                            DrawTransactionsPage(pdf, report);
                    }
                    else if (reportType == "Только график")
                    {
                        if (report.IncomeBreakdown?.Count > 0 || report.ExpenseBreakdown?.Count > 0)
                        {
                            DrawChartsPage(pdf, report);
                        }
                    }
                    else if (reportType == "Детализация по категориям")
                    {
                        if (report.ExpenseBreakdown?.Count > 0)
                            DrawExpenseCategoriesPage(pdf, report);
                        if (report.IncomeBreakdown?.Count > 0)
                            DrawIncomeCategoriesPage(pdf, report);
                    }
                    else if (reportType == "Все операции")
                    {
                        if (report.Transactions?.Count > 0)
                            DrawTransactionsPage(pdf, report);
                    }

                    if (report.MonthlyTrends?.Count > 0 &&
                        (reportType == "Текущий отчет" || reportType == "Полный отчет за период"))
                    {
                        DrawTrendsPage(pdf, report);
                    }

                    pdf.Close();
                }

                // Сбрасываем позицию потока
                memoryStream.Position = 0;

                // Сохраняем через FileSaver
                var result = await _fileSaver.SaveAsync(fileName, memoryStream, new CancellationToken());

                if (result.IsSuccessful)
                {
                    await Shell.Current.DisplayAlertAsync("Успех",
                        $"Отчет успешно сохранен:\n{result.FilePath}", "OK");
                    return true;
                }
                else
                {
                    if (result.Exception != null)
                        Debug.WriteLine($"Ошибка: {result.Exception.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при экспорте PDF: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить PDF: {ex.Message}", "OK");
                return false;
            }
        }

        private void DrawCoverPage(SKDocument pdf, ReportData report, DateTime startDate, DateTime endDate)
        {
            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 36,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Финансовый отчет", PageWidth / 2, 100, titlePaint);
                }

                // Подзаголовок с периодом
                using (var periodPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 20,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText($"Период: {startDate:d} — {endDate:d}", PageWidth / 2, 160, periodPaint);
                }

                // Количество операций
                using (var countPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 16,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText($"Всего операций: {report.Transactions?.Count ?? 0}", PageWidth / 2, 190, countPaint);
                }

                // Линия-разделитель
                using (var linePaint = new SKPaint
                {
                    Color = SKColors.LightGray,
                    StrokeWidth = 2,
                    Style = SKPaintStyle.Stroke
                })
                {
                    canvas.DrawLine(Margin, 230, PageWidth - Margin, 230, linePaint);
                }

                // Блок с итогами
                DrawSummaryBox(canvas, report, 280);

                // Дата генерации
                using (var datePaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 14,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Right
                })
                {
                    canvas.DrawText($"Сгенерировано: {DateTime.Now:g}", PageWidth - Margin, PageHeight - 50, datePaint);
                }

                // Версия приложения
                using (var versionPaint = new SKPaint
                {
                    Color = SKColors.LightGray,
                    TextSize = 12,
                    IsAntialias = true
                })
                {
                    canvas.DrawText("Finly - Умный финансовый помощник", Margin, PageHeight - 50, versionPaint);
                }
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawSummaryBox(SKCanvas canvas, ReportData report, float y)
        {
            var boxWidth = PageWidth - 2 * Margin;
            var boxHeight = 150;

            // Фон для блока
            using (var backgroundPaint = new SKPaint
            {
                Color = SKColor.Parse("#F5F7FA"),
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRect(Margin, y, boxWidth, boxHeight, backgroundPaint);
            }

            // Рамка
            using (var borderPaint = new SKPaint
            {
                Color = SKColor.Parse("#E0E0E0"),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            })
            {
                canvas.DrawRect(Margin, y, boxWidth, boxHeight, borderPaint);
            }

            // Заголовок блока
            using (var headerPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 18,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("Сводка", Margin + 20, y + 35, headerPaint);
            }

            var startX = Margin + 40;
            var textY = y + 80;

            // Доходы
            DrawStatItem(canvas, "Доходы:", report.TotalIncome, SKColors.Green, startX, textY);

            // Расходы
            DrawStatItem(canvas, "Расходы:", report.TotalExpenses, SKColors.Red, startX + 250, textY);

            // Сбережения
            DrawStatItem(canvas, "Сбережения:", report.NetSavings,
                report.NetSavings >= 0 ? SKColors.Blue : SKColors.Red, startX + 500, textY);
        }

        private void DrawStatItem(SKCanvas canvas, string label, decimal value, SKColor color, float x, float y)
        {
            using (var labelPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 14,
                IsAntialias = true
            })
            {
                canvas.DrawText(label, x, y, labelPaint);
            }

            using (var valuePaint = new SKPaint
            {
                Color = color,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText(FormatAmount(value), x, y + 35, valuePaint);
            }
        }

        private string FormatAmount(decimal amount)
        {
            if (amount >= 1_000_000_000)
                return $"{amount / 1_000_000_000m:F2} млрд ₽";
            if (amount >= 1_000_000)
                return $"{amount / 1_000_000m:F2} млн ₽";
            if (amount >= 1_000)
                return $"{amount / 1_000m:F2} тыс ₽";
            return amount.ToString("C0");
        }

        private void DrawChartsPage(SKDocument pdf, ReportData report)
        {
            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок страницы
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Анализ доходов и расходов", Margin, 70, titlePaint);
                }

                float currentY = 120;

                // Если есть расходы, рисуем круговую диаграмму расходов
                if (report.ExpenseBreakdown?.Count > 0)
                {
                    using (var subtitlePaint = new SKPaint
                    {
                        Color = SKColors.Red,
                        TextSize = 18,
                        IsAntialias = true,
                        Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                    })
                    {
                        canvas.DrawText("Расходы по категориям", Margin, currentY, subtitlePaint);
                    }

                    DrawPieChartForPdf(canvas, report.ExpenseBreakdown, currentY + 20);
                    currentY += 300;
                }

                // Если есть доходы, рисуем круговую диаграмму доходов
                if (report.IncomeBreakdown?.Count > 0)
                {
                    using (var subtitlePaint = new SKPaint
                    {
                        Color = SKColors.Green,
                        TextSize = 18,
                        IsAntialias = true,
                        Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                    })
                    {
                        canvas.DrawText("Доходы по категориям", Margin, currentY, subtitlePaint);
                    }

                    DrawPieChartForPdf(canvas, report.IncomeBreakdown, currentY + 20);
                }
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawPieChartForPdf(SKCanvas canvas, ObservableCollection<CategoryBreakdownItem> items, float topY)
        {
            var centerX = PageWidth / 2f;
            var centerY = topY + 120;
            var radius = 150;

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
                var sweepAngle = (float)(item.Percentage * 3.6);
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

            // Обводка
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                IsAntialias = true
            };
            canvas.DrawCircle(centerX, centerY, radius, strokePaint);

            // Легенда
            DrawLegendForPdf(canvas, items, centerX + radius + 50, centerY - 100);
        }

        private void DrawLegendForPdf(SKCanvas canvas, ObservableCollection<CategoryBreakdownItem> items, float startX, float startY)
        {
            var spacing = 25f;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true
            };

            for (int i = 0; i < Math.Min(items.Count, 8); i++)
            {
                var item = items[i];
                var y = startY + i * spacing;

                // Цветной квадрат
                using var rectPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColor.Parse(item.CategoryColor)
                };
                canvas.DrawRect(startX, y, 15, 15, rectPaint);

                // Текст
                var shortName = item.CategoryName.Length > 20
                    ? item.CategoryName.Substring(0, 20) + ".."
                    : item.CategoryName;

                canvas.DrawText($"{shortName} ({item.Percentage:F1}%)",
                    startX + 20, y + 12, textPaint);
            }
        }

        private void DrawIncomeCategoriesPage(SKDocument pdf, ReportData report)
        {
            if (report.IncomeBreakdown?.Count == 0)
                return;

            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок страницы
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Green,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Детализация доходов", Margin, 70, titlePaint);
                }

                // Таблица с категориями
                DrawCategoriesTable(canvas, report.IncomeBreakdown, 120, true);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawExpenseCategoriesPage(SKDocument pdf, ReportData report)
        {
            if (report.ExpenseBreakdown?.Count == 0)
                return;

            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок страницы
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Red,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Детализация расходов", Margin, 70, titlePaint);
                }

                // Таблица с категориями
                DrawCategoriesTable(canvas, report.ExpenseBreakdown, 120, false);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawCategoriesTable(SKCanvas canvas, ObservableCollection<CategoryBreakdownItem> items, float topY, bool isIncome)
        {
            var colWidths = new float[] { 80, 300, 200, 150 };
            var startX = Margin;
            var currentY = topY;

            // Заголовки таблицы
            using (var headerPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 14,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                // Фон заголовков
                using var headerBg = new SKPaint
                {
                    Color = isIncome ? SKColor.Parse("#2E7D32") : SKColor.Parse("#C62828"),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(startX, currentY - 25, PageWidth - 2 * Margin, 30, headerBg);

                // Текст заголовков
                canvas.DrawText("№", startX + 30, currentY, headerPaint);
                canvas.DrawText("Категория", startX + colWidths[0] + 30, currentY, headerPaint);
                canvas.DrawText("Сумма", startX + colWidths[0] + colWidths[1] + 30, currentY, headerPaint);
                canvas.DrawText("%", startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, headerPaint);
            }

            currentY += 20;

            // Строки таблицы
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Чередование фона
                if (i % 2 == 0)
                {
                    using var rowBg = new SKPaint
                    {
                        Color = SKColor.Parse("#F5F5F5"),
                        Style = SKPaintStyle.Fill
                    };
                    canvas.DrawRect(startX, currentY - 15, PageWidth - 2 * Margin, 30, rowBg);
                }

                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 12,
                    IsAntialias = true
                };

                canvas.DrawText((i + 1).ToString(), startX + 30, currentY, textPaint);
                canvas.DrawText(item.CategoryName, startX + colWidths[0] + 30, currentY, textPaint);
                canvas.DrawText(FormatAmount(item.Amount), startX + colWidths[0] + colWidths[1] + 30, currentY, textPaint);
                canvas.DrawText($"{item.Percentage:F1}%", startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, textPaint);

                currentY += 30;

                // Если страница заканчивается
                if (currentY > PageHeight - 100)
                {
                    // TODO: Добавить новую страницу для продолжения таблицы
                    break;
                }
            }
        }

        private void DrawTrendsPage(SKDocument pdf, ReportData report)
        {
            if (report.MonthlyTrends?.Count == 0)
                return;

            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок страницы
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Динамика по месяцам", Margin, 70, titlePaint);
                }

                // Таблица с данными
                DrawTrendsTable(canvas, report, 120);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawTrendsTable(SKCanvas canvas, ReportData report, float topY)
        {
            var colWidths = new float[] { 150, 200, 200, 200 };
            var startX = Margin;
            var currentY = topY;

            // Заголовки
            using (var headerPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 14,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                using var headerBg = new SKPaint
                {
                    Color = SKColor.Parse("#6200EA"),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(startX, currentY - 25, PageWidth - 2 * Margin, 30, headerBg);

                canvas.DrawText("Месяц", startX + 30, currentY, headerPaint);
                canvas.DrawText("Доходы", startX + colWidths[0] + 30, currentY, headerPaint);
                canvas.DrawText("Расходы", startX + colWidths[0] + colWidths[1] + 30, currentY, headerPaint);
                canvas.DrawText("Сбережения", startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, headerPaint);
            }

            currentY += 20;

            foreach (var trend in report.MonthlyTrends)
            {
                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 12,
                    IsAntialias = true
                };

                canvas.DrawText(trend.MonthName, startX + 30, currentY, textPaint);
                canvas.DrawText(FormatAmount(trend.Income), startX + colWidths[0] + 30, currentY, textPaint);
                canvas.DrawText(FormatAmount(trend.Expenses), startX + colWidths[0] + colWidths[1] + 30, currentY, textPaint);

                using var savingsPaint = new SKPaint
                {
                    Color = trend.Savings >= 0 ? SKColors.Green : SKColors.Red,
                    TextSize = 12,
                    IsAntialias = true
                };
                canvas.DrawText(FormatAmount(trend.Savings), startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, savingsPaint);

                currentY += 25;
            }
        }

        private void DrawTransactionsPage(SKDocument pdf, ReportData report)
        {
            if (report.Transactions?.Count == 0)
                return;

            var canvas = pdf.BeginPage(PageWidth, PageHeight);

            try
            {
                // Заголовок страницы
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText("Все операции за период", Margin, 70, titlePaint);
                }

                // Количество операций
                using (var countPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 14,
                    IsAntialias = true
                })
                {
                    canvas.DrawText($"Всего операций: {report.Transactions.Count}", Margin, 100, countPaint);
                }

                // Таблица с операциями
                DrawTransactionsTable(canvas, report, 130);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawTransactionsTable(SKCanvas canvas, ReportData report, float topY)
        {
            var colWidths = new float[] { 60, 100, 80, 150, 150, 200, 120 };
            var startX = Margin;
            var currentY = topY;

            // Заголовки таблицы
            using (var headerPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 11,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                // Фон заголовков
                using var headerBg = new SKPaint
                {
                    Color = SKColor.Parse("#6200EA"),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(startX, currentY - 20, PageWidth - 2 * Margin, 25, headerBg);

                // Текст заголовков
                canvas.DrawText("№", startX + 20, currentY, headerPaint);
                canvas.DrawText("Дата", startX + colWidths[0] + 20, currentY, headerPaint);
                canvas.DrawText("Тип", startX + colWidths[0] + colWidths[1] + 20, currentY, headerPaint);
                canvas.DrawText("Категория", startX + colWidths[0] + colWidths[1] + colWidths[2] + 20, currentY, headerPaint);
                canvas.DrawText("Счет", startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + 20, currentY, headerPaint);
                canvas.DrawText("Описание", startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4] + 20, currentY, headerPaint);
                canvas.DrawText("Сумма", startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4] + colWidths[5] + 20, currentY, headerPaint);
            }

            currentY += 15;

            // Строки таблицы
            int row = 0;
            foreach (var item in report.Transactions.OrderByDescending(t => t.Date))
            {
                if (currentY > PageHeight - 50)
                {
                    // TODO: Добавить новую страницу для продолжения таблицы
                    break;
                }

                // Чередование фона
                if (row % 2 == 0)
                {
                    using var rowBg = new SKPaint
                    {
                        Color = SKColor.Parse("#F5F5F5"),
                        Style = SKPaintStyle.Fill
                    };
                    canvas.DrawRect(startX, currentY - 12, PageWidth - 2 * Margin, 22, rowBg);
                }

                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 10,
                    IsAntialias = true
                };

                // Номер
                canvas.DrawText((row + 1).ToString(), startX + 20, currentY, textPaint);

                // Дата
                canvas.DrawText(item.Date.ToString("dd.MM.yyyy"), startX + colWidths[0] + 20, currentY, textPaint);

                // Тип
                canvas.DrawText(item.Type == TransactionType.Income ? "Доход" : "Расход",
                    startX + colWidths[0] + colWidths[1] + 20, currentY, textPaint);

                // Категория
                var categoryName = item.CategoryName.Length > 15
                    ? item.CategoryName.Substring(0, 15) + "..."
                    : item.CategoryName;
                canvas.DrawText(categoryName, startX + colWidths[0] + colWidths[1] + colWidths[2] + 20, currentY, textPaint);

                // Счет
                var accountName = item.AccountName.Length > 15
                    ? item.AccountName.Substring(0, 15) + "..."
                    : item.AccountName;
                canvas.DrawText(accountName, startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + 20, currentY, textPaint);

                // Описание
                var description = item.Description.Length > 20
                    ? item.Description.Substring(0, 20) + "..."
                    : item.Description;
                canvas.DrawText(description, startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4] + 20, currentY, textPaint);

                // Сумма
                using var amountPaint = new SKPaint
                {
                    Color = item.Type == TransactionType.Income ? SKColors.Green : SKColors.Red,
                    TextSize = 10,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                };
                canvas.DrawText(FormatAmount(item.Amount),
                    startX + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4] + colWidths[5] + 20,
                    currentY, amountPaint);

                currentY += 22;
                row++;
            }
        }
    }
}