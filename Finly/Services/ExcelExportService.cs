using CommunityToolkit.Maui.Storage;
using Finly.Models;
using Finly.ViewModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Diagnostics;
using System.Drawing;
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

                    // Доходы
                    if (report.IncomeBreakdown?.Count > 0)
                        CreateIncomeSheet(package, report);

                    // Расходы
                    if (report.ExpenseBreakdown?.Count > 0)
                        CreateExpenseSheet(package, report);

                    if (report.MonthlyTrends?.Count > 0)
                        CreateTrendsSheet(package, report);

                    // ВСЕГДА добавляем лист с транзакциями для полного отчета
                    if (report.Transactions?.Count > 0)
                        CreateTransactionsSheet(package, report);
                }
                else if (reportType == "Только график")
                {
                    if (report.ExpenseBreakdown?.Count > 0)
                        CreateExpenseSheet(package, report);
                    if (report.IncomeBreakdown?.Count > 0)
                        CreateIncomeSheet(package, report);
                }
                else if (reportType == "Детализация по категориям")
                {
                    if (report.ExpenseBreakdown?.Count > 0)
                        CreateExpenseSheet(package, report);
                    if (report.IncomeBreakdown?.Count > 0)
                        CreateIncomeSheet(package, report);
                }
                else if (reportType == "Все операции")
                {
                    if (report.Transactions?.Count > 0)
                        CreateTransactionsSheet(package, report);
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

            // Количество операций
            sheet.Cells["A9"].Value = "Всего операций:";
            sheet.Cells["B9"].Value = report.Transactions?.Count ?? 0;
            sheet.Cells["A9:B9"].Style.Font.Bold = true;

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

        private void CreateIncomeSheet(ExcelPackage package, ReportData report)
        {
            if (report.IncomeBreakdown?.Count == 0)
                return;

            var sheet = package.Workbook.Worksheets.Add("Доходы");

            // Заголовок
            sheet.Cells["A1"].Value = "Детализация доходов";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(46, 125, 50)); // Темно-зеленый
            sheet.Cells["A1:D1"].Merge = true;

            // Заголовки таблицы
            var headers = new[] { "№", "Категория", "Сумма", "%" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 125, 50));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 4;
            foreach (var item in report.IncomeBreakdown)
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
                    sheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(232, 245, 233)); // Светло-зеленый
                }

                row++;
            }

            // Итог
            sheet.Cells[row, 2].Value = "ИТОГО:";
            sheet.Cells[row, 2].Style.Font.Bold = true;

            var totalSum = report.IncomeBreakdown.Sum(c => c.Amount);
            sheet.Cells[row, 3].Value = totalSum;
            sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 3].Style.Font.Bold = true;

            sheet.Cells[row, 4].Value = 1.0;
            sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            sheet.Cells[row, 4].Style.Font.Bold = true;

            // Форматирование
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateExpenseSheet(ExcelPackage package, ReportData report)
        {
            if (report.ExpenseBreakdown?.Count == 0)
                return;

            var sheet = package.Workbook.Worksheets.Add("Расходы");

            // Заголовок
            sheet.Cells["A1"].Value = "Детализация расходов";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(198, 40, 40)); // Темно-красный
            sheet.Cells["A1:D1"].Merge = true;

            // Заголовки таблицы
            var headers = new[] { "№", "Категория", "Сумма", "%" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 40, 40));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 4;
            foreach (var item in report.ExpenseBreakdown)
            {
                sheet.Cells[row, 1].Value = row - 3;
                sheet.Cells[row, 2].Value = item.CategoryName;
                sheet.Cells[row, 3].Value = item.Amount;
                sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                sheet.Cells[row, 4].Value = item.Percentage / 100;
                sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
                sheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Чередование фона
                if ((row - 4) % 2 == 0)
                {
                    sheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 235, 238)); // Светло-красный
                }

                row++;
            }

            // Итог
            sheet.Cells[row, 2].Value = "ИТОГО:";
            sheet.Cells[row, 2].Style.Font.Bold = true;

            var totalSum = report.ExpenseBreakdown.Sum(c => c.Amount);
            sheet.Cells[row, 3].Value = totalSum;
            sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 3].Style.Font.Bold = true;

            sheet.Cells[row, 4].Value = 1.0;
            sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            sheet.Cells[row, 4].Style.Font.Bold = true;

            // Форматирование
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
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

        private void CreateTransactionsSheet(ExcelPackage package, ReportData report)
        {
            if (report.Transactions?.Count == 0)
                return;

            var sheet = package.Workbook.Worksheets.Add("Операции");

            // Заголовок
            sheet.Cells["A1"].Value = "Все операции за период";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(98, 0, 234));
            sheet.Cells["A1:G1"].Merge = true;

            sheet.Cells["A2"].Value = $"Всего операций: {report.Transactions.Count}";
            sheet.Cells["A2"].Style.Font.Size = 12;
            sheet.Cells["A2"].Style.Font.Color.SetColor(Color.Gray);
            sheet.Cells["A2:G2"].Merge = true;

            // Заголовки таблицы
            var headers = new[] { "№", "Дата", "Тип", "Категория", "Счет", "Описание", "Сумма" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[4, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(98, 0, 234));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 5;
            foreach (var item in report.Transactions.OrderByDescending(t => t.Date))
            {
                sheet.Cells[row, 1].Value = row - 4;

                // Дата
                sheet.Cells[row, 2].Value = item.Date;
                sheet.Cells[row, 2].Style.Numberformat.Format = "dd.MM.yyyy HH:mm";

                // Тип
                sheet.Cells[row, 3].Value = item.Type == TransactionType.Income ? "Доход" : "Расход";
                sheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Категория
                sheet.Cells[row, 4].Value = item.CategoryName;

                // Счет
                sheet.Cells[row, 5].Value = item.AccountName;

                // Описание
                sheet.Cells[row, 6].Value = item.Description;

                // Сумма
                sheet.Cells[row, 7].Value = item.Amount;
                sheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";
                sheet.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                sheet.Cells[row, 7].Style.Font.Color.SetColor(
                    item.Type == TransactionType.Income ? Color.Green : Color.Red);

                // Чередование фона
                if ((row - 5) % 2 == 0)
                {
                    sheet.Cells[row, 1, row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 1, row, 7].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 245, 245));
                }

                row++;
            }

            // Итог
            sheet.Cells[row, 6].Value = "ИТОГО:";
            sheet.Cells[row, 6].Style.Font.Bold = true;

            var totalIncome = report.Transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);
            var totalExpenses = report.Transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            sheet.Cells[row, 7].Value = totalIncome - totalExpenses;
            sheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";
            sheet.Cells[row, 7].Style.Font.Bold = true;
            sheet.Cells[row, 7].Style.Font.Color.SetColor(
                totalIncome - totalExpenses >= 0 ? Color.Green : Color.Red);

            // Автоширина
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Добавляем примечание о больших числах
            if (report.Transactions.Any(t => Math.Abs(t.Amount) >= 1_000_000))
            {
                row += 2;
                sheet.Cells[row, 1].Value = "* Все суммы указаны в рублях. Большие числа могут быть сокращены в отображении, но сохранены полностью.";
                sheet.Cells[row, 1, row, 7].Merge = true;
                sheet.Cells[row, 1].Style.Font.Size = 9;
                sheet.Cells[row, 1].Style.Font.Italic = true;
                sheet.Cells[row, 1].Style.Font.Color.SetColor(Color.Gray);
            }
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