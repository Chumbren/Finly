using Finly.ViewModels;
using SkiaSharp;
using System.Diagnostics;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;

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
                        DrawChartsPage(pdf, report);
                        DrawCategoriesPage(pdf, report);
                    }
                    else if (reportType == "Только график")
                    {
                        DrawChartsPage(pdf, report);
                    }
                    else if (reportType == "Детализация по категориям")
                    {
                        DrawCategoriesPage(pdf, report);
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

                // Сохраняем через FileSaver - открывает системный диалог сохранения
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

        private async Task<bool> SaveUsingFilePicker(string fileName, ReportData report, DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                // Для сохранения файла используем другой подход
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".pdf" } },
                        { DevicePlatform.Android, new[] { "application/pdf" } },
                        { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                        { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Сохранить отчет в PDF",
                    FileTypes = customFileType,
                };

                // На некоторых платформах FilePicker не поддерживает сохранение
                // Поэтому используем временный файл
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Сохраняем во временный файл
                var result = await SavePdfToStream(tempPath, report, startDate, endDate, reportType);

                if (result)
                {
                    // На Android и iOS показываем где файл сохранен
                    await Shell.Current.DisplayAlertAsync("Успех",
                        $"Отчет сохранен во временную папку:\n{tempPath}\n\nВы можете найти его через файловый менеджер.", "OK");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SavePdfToStream(string filePath, ReportData report, DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                using var stream = File.OpenWrite(filePath);
                using var pdf = SKDocument.CreatePdf(stream);

                DrawCoverPage(pdf, report, startDate, endDate);

                if (reportType == "Текущий отчет" || reportType == "Полный отчет за период")
                {
                    DrawChartsPage(pdf, report);
                    DrawCategoriesPage(pdf, report);
                }
                else if (reportType == "Только график")
                {
                    DrawChartsPage(pdf, report);
                }
                else if (reportType == "Детализация по категориям")
                {
                    DrawCategoriesPage(pdf, report);
                }

                if (report.MonthlyTrends?.Count > 0 &&
                    (reportType == "Текущий отчет" || reportType == "Полный отчет за период"))
                {
                    DrawTrendsPage(pdf, report);
                }

                pdf.Close();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при создании PDF: {ex.Message}");
                throw;
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

                // Линия-разделитель
                using (var linePaint = new SKPaint
                {
                    Color = SKColors.LightGray,
                    StrokeWidth = 2,
                    Style = SKPaintStyle.Stroke
                })
                {
                    canvas.DrawLine(Margin, 200, PageWidth - Margin, 200, linePaint);
                }

                // Блок с итогами
                DrawSummaryBox(canvas, report, 250);

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
                canvas.DrawText(value.ToString("C0"), x, y + 35, valuePaint);
            }
        }

        private void DrawChartsPage(SKDocument pdf, ReportData report)
        {
            if (report.CategoryBreakdown?.Count == 0)
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
                    canvas.DrawText("Анализ расходов по категориям", Margin, 70, titlePaint);
                }

                // Круговая диаграмма
                DrawPieChartForPdf(canvas, report, 150);

                // Легенда
                DrawLegendForPdf(canvas, report, 550);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawPieChartForPdf(SKCanvas canvas, ReportData report, float topY)
        {
            var centerX = PageWidth / 2f;
            var centerY = topY + 150;
            var radius = 180;

            float startAngle = -90;

            // Рисуем сегменты
            foreach (var item in report.CategoryBreakdown)
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
        }

        private void DrawLegendForPdf(SKCanvas canvas, ReportData report, float startY)
        {
            var legendX = 200f;
            var spacing = 35f;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 14,
                IsAntialias = true
            };

            for (int i = 0; i < report.CategoryBreakdown.Count; i++)
            {
                var item = report.CategoryBreakdown[i];
                var y = startY + i * spacing;

                // Цветной квадрат
                using var rectPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColor.Parse(item.CategoryColor)
                };
                canvas.DrawRect(legendX, y, 25, 25, rectPaint);

                // Текст
                canvas.DrawText($"{item.CategoryName}: {item.Amount:C0} ({item.Percentage:F1}%)",
                    legendX + 35, y + 18, textPaint);
            }
        }

        private void DrawCategoriesPage(SKDocument pdf, ReportData report)
        {
            if (report.CategoryBreakdown?.Count == 0)
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
                    canvas.DrawText("Детализация по категориям", Margin, 70, titlePaint);
                }

                // Таблица с категориями
                DrawCategoriesTable(canvas, report, 120);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawCategoriesTable(SKCanvas canvas, ReportData report, float topY)
        {
            var colWidths = new float[] { 80, 250, 150, 150 };
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
                    Color = SKColor.Parse("#6200EA"),
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
            for (int i = 0; i < report.CategoryBreakdown.Count; i++)
            {
                var item = report.CategoryBreakdown[i];

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
                canvas.DrawText(item.Amount.ToString("C0"), startX + colWidths[0] + colWidths[1] + 30, currentY, textPaint);
                canvas.DrawText($"{item.Percentage:F1}%", startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, textPaint);

                currentY += 30;

                // Если страница заканчивается
                if (currentY > PageHeight - 100)
                {
                    // TODO: Добавить новую страницу
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

                // Рисуем линейный график
                DrawLineChartForPdf(canvas, report, 120);

                // Таблица с данными
                DrawTrendsTable(canvas, report, 500);
            }
            finally
            {
                pdf.EndPage();
            }
        }

        private void DrawLineChartForPdf(SKCanvas canvas, ReportData report, float topY)
        {
            var chartWidth = PageWidth - 2 * Margin - 100;
            var chartHeight = 250;
            var leftX = Margin + 50;
            var bottomY = topY + chartHeight;

            var trends = report.MonthlyTrends.ToList();
            if (trends.Count < 2) return;

            // Находим максимум
            var maxValue = (float)Math.Max(trends.Max(t => t.Income), trends.Max(t => t.Expenses));
            if (maxValue == 0) maxValue = 1;

            var scale = chartHeight / maxValue;
            var stepX = chartWidth / (trends.Count - 1);

            // Рисуем сетку
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
                    canvas.DrawLine(leftX, y, leftX + chartWidth, y, gridPaint);
                }
            }

            // Собираем точки
            var incomePoints = new List<SKPoint>();
            var expensePoints = new List<SKPoint>();

            for (int i = 0; i < trends.Count; i++)
            {
                var x = leftX + i * stepX;
                incomePoints.Add(new SKPoint(x, bottomY - (float)trends[i].Income * scale));
                expensePoints.Add(new SKPoint(x, bottomY - (float)trends[i].Expenses * scale));
            }

            // Рисуем линии
            DrawLineSeries(canvas, incomePoints, SKColors.Green);
            DrawLineSeries(canvas, expensePoints, SKColors.Red);

            // Подписи месяцев
            using (var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 10,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            })
            {
                for (int i = 0; i < trends.Count; i++)
                {
                    var x = leftX + i * stepX;
                    canvas.DrawText(trends[i].MonthName, x, bottomY + 20, textPaint);
                }
            }
        }

        private void DrawLineSeries(SKCanvas canvas, List<SKPoint> points, SKColor color)
        {
            if (points.Count < 2) return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color,
                StrokeWidth = 2,
                IsAntialias = true
            };

            for (int i = 0; i < points.Count - 1; i++)
            {
                canvas.DrawLine(points[i], points[i + 1], paint);
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
                canvas.DrawText(trend.Income.ToString("C0"), startX + colWidths[0] + 30, currentY, textPaint);
                canvas.DrawText(trend.Expenses.ToString("C0"), startX + colWidths[0] + colWidths[1] + 30, currentY, textPaint);

                using var savingsPaint = new SKPaint
                {
                    Color = trend.Savings >= 0 ? SKColors.Green : SKColors.Red,
                    TextSize = 12,
                    IsAntialias = true
                };
                canvas.DrawText(trend.Savings.ToString("C0"), startX + colWidths[0] + colWidths[1] + colWidths[2] + 30, currentY, savingsPaint);

                currentY += 25;
            }
        }
    }
}