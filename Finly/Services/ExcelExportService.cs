using Finly.ViewModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Diagnostics;
using System.Drawing;
using CommunityToolkit.Maui.Storage;
using Color = System.Drawing.Color;

namespace Finly.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public ExcelExportService()
        {
            // Устанавливаем лицензионный контекст для новых версий EPPlus
            ExcelPackage.License.SetNonCommercialPersonal("sovesnov-kp");
        }

        public async Task<bool> ExportReportToExcelAsync(ReportData report, DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                var fileName = $"Financial_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

                // Создаем Excel пакет
                using var package = new ExcelPackage();

                // Создаем страницы в зависимости от типа отчета
                if (reportType == "Текущий отчет" || reportType == "Полный отчет за период")
                {
                    CreateSummarySheet(package, report, startDate, endDate);
                    CreateCategoriesSheet(package, report);

                    if (report.MonthlyTrends?.Count > 0)
                        CreateTrendsSheet(package, report);
                }
                else if (reportType == "Только график")
                {
                    CreateCategoriesSheet(package, report);
                }
                else if (reportType == "Детализация по категориям")
                {
                    CreateCategoriesSheet(package, report);
                }

                // Сохраняем файл
                return await SaveExcelFile(package, fileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при экспорте Excel: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить Excel: {ex.Message}", "OK");
                return false;
            }
        }

        private void CreateSummarySheet(ExcelPackage package, ReportData report, DateTime startDate, DateTime endDate)
        {
            var sheet = package.Workbook.Worksheets.Add("Сводка");

            // Заголовок
            sheet.Cells["A1"].Value = "Финансовый отчет";
            sheet.Cells["A1"].Style.Font.Size = 18;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(98, 0, 234));
            sheet.Cells["A1:C1"].Merge = true;

            // Период
            sheet.Cells["A2"].Value = $"Период: {startDate:d} — {endDate:d}";
            sheet.Cells["A2"].Style.Font.Size = 12;
            sheet.Cells["A2"].Style.Font.Color.SetColor(Color.Gray);
            sheet.Cells["A2:C2"].Merge = true;

            // Заголовки таблицы
            sheet.Cells["A4"].Value = "Показатель";
            sheet.Cells["B4"].Value = "Сумма";
            sheet.Cells["A4:B4"].Style.Font.Bold = true;
            sheet.Cells["A4:B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells["A4:B4"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(98, 0, 234));
            sheet.Cells["A4:B4"].Style.Font.Color.SetColor(Color.White);
            sheet.Cells["A4:B4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Данные
            sheet.Cells["A5"].Value = "Доходы";
            sheet.Cells["B5"].Value = report.TotalIncome;
            sheet.Cells["B5"].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells["B5"].Style.Font.Color.SetColor(Color.Green);

            sheet.Cells["A6"].Value = "Расходы";
            sheet.Cells["B6"].Value = report.TotalExpenses;
            sheet.Cells["B6"].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells["B6"].Style.Font.Color.SetColor(Color.Red);

            sheet.Cells["A7"].Value = "Сбережения";
            sheet.Cells["B7"].Value = report.NetSavings;
            sheet.Cells["B7"].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells["B7"].Style.Font.Color.SetColor(report.NetSavings >= 0 ? Color.Green : Color.Red);

            // Итоговая строка
            sheet.Cells["A8:B8"].Style.Font.Bold = true;
            sheet.Cells["A8:B8"].Style.Border.Top.Style = ExcelBorderStyle.Thin;

            // Автоширина колонок
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Добавляем примечание
            sheet.Cells["A10"].Value = $"Сгенерировано: {DateTime.Now:g}";
            sheet.Cells["A10"].Style.Font.Size = 10;
            sheet.Cells["A10"].Style.Font.Color.SetColor(Color.Gray);
            sheet.Cells["A10:C10"].Merge = true;
        }

        private void CreateCategoriesSheet(ExcelPackage package, ReportData report)
        {
            if (report.CategoryBreakdown?.Count == 0)
                return;

            var sheet = package.Workbook.Worksheets.Add("Категории");

            // Заголовок
            sheet.Cells["A1"].Value = "Детализация по категориям";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(98, 0, 234));
            sheet.Cells["A1:D1"].Merge = true;

            // Заголовки таблицы
            var headers = new[] { "№", "Категория", "Сумма", "%" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(98, 0, 234));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 4;
            foreach (var item in report.CategoryBreakdown)
            {
                sheet.Cells[row, 1].Value = row - 3;
                sheet.Cells[row, 2].Value = item.CategoryName;
                sheet.Cells[row, 3].Value = item.Amount;
                sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                sheet.Cells[row, 4].Value = item.Percentage / 100; // В Excel проценты хранятся как доли
                sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
                sheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Чередование фона
                if ((row - 4) % 2 == 0)
                {
                    sheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 245, 245));
                }

                row++;
            }

            // Итог
            sheet.Cells[row, 2].Value = "ИТОГО:";
            sheet.Cells[row, 2].Style.Font.Bold = true;

            var totalSum = report.CategoryBreakdown.Sum(c => c.Amount);
            sheet.Cells[row, 3].Value = totalSum;
            sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 3].Style.Font.Bold = true;

            sheet.Cells[row, 4].Value = 1.0;
            sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            sheet.Cells[row, 4].Style.Font.Bold = true;

            // Форматирование
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Добавляем примечание о больших числах
            if (report.CategoryBreakdown.Any(c => Math.Abs(c.Amount) >= 1_000_000))
            {
                row += 2;
                sheet.Cells[row, 1].Value = "* Все суммы указаны в рублях. Большие числа могут быть сокращены в отображении, но сохранены полностью.";
                sheet.Cells[row, 1, row, 4].Merge = true;
                sheet.Cells[row, 1].Style.Font.Size = 9;
                sheet.Cells[row, 1].Style.Font.Italic = true;
                sheet.Cells[row, 1].Style.Font.Color.SetColor(Color.Gray);
            }
        }

        private void CreateTrendsSheet(ExcelPackage package, ReportData report)
        {
            if (report.MonthlyTrends?.Count == 0)
                return;

            var sheet = package.Workbook.Worksheets.Add("Динамика");

            // Заголовок
            sheet.Cells["A1"].Value = "Динамика по месяцам";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(98, 0, 234));
            sheet.Cells["A1:D1"].Merge = true;

            // Заголовки таблицы
            var headers = new[] { "Месяц", "Доходы", "Расходы", "Сбережения" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(98, 0, 234));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 4;
            foreach (var item in report.MonthlyTrends)
            {
                sheet.Cells[row, 1].Value = item.MonthName;

                sheet.Cells[row, 2].Value = item.Income;
                sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                sheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);

                sheet.Cells[row, 3].Value = item.Expenses;
                sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                sheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Red);

                sheet.Cells[row, 4].Value = item.Savings;
                sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                sheet.Cells[row, 4].Style.Font.Color.SetColor(item.Savings >= 0 ? Color.Green : Color.Red);

                // Чередование фона
                if ((row - 4) % 2 == 0)
                {
                    sheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 245, 245));
                }

                row++;
            }

            // Итог
            sheet.Cells[row, 1].Value = "ВСЕГО:";
            sheet.Cells[row, 1].Style.Font.Bold = true;

            var totalIncome = report.MonthlyTrends.Sum(t => t.Income);
            var totalExpenses = report.MonthlyTrends.Sum(t => t.Expenses);
            var totalSavings = report.MonthlyTrends.Sum(t => t.Savings);

            sheet.Cells[row, 2].Value = totalIncome;
            sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 2].Style.Font.Bold = true;

            sheet.Cells[row, 3].Value = totalExpenses;
            sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 3].Style.Font.Bold = true;

            sheet.Cells[row, 4].Value = totalSavings;
            sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 4].Style.Font.Bold = true;
            sheet.Cells[row, 4].Style.Font.Color.SetColor(totalSavings >= 0 ? Color.Green : Color.Red);

            // Автоширина
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private async Task<bool> SaveExcelFile(ExcelPackage package, string fileName)
        {
            try
            {
                // Используем FileSaver из CommunityToolkit
                var fileSaver = new FileSaverImplementation();

                using var stream = new MemoryStream();
                await package.SaveAsAsync(stream);
                stream.Position = 0;

                var result = await fileSaver.SaveAsync(fileName, stream, new CancellationToken());

                if (result.IsSuccessful)
                {
                    await Shell.Current.DisplayAlertAsync("Успех",
                        $"Excel файл успешно сохранен:\n{result.FilePath}", "OK");
                    return true;
                }
                else
                {
                    if (result.Exception != null)
                        Debug.WriteLine($"Ошибка сохранения: {result.Exception.Message}");

                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Не удалось сохранить файл", "OK");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении Excel: {ex.Message}");
                throw;
            }
        }
    }
}