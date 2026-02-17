using Finly.ViewModels;

namespace Finly.Services
{
    public interface IExcelExportService
    {
        Task<bool> ExportReportToExcelAsync(ReportData report, DateTime startDate, DateTime endDate, string reportType);
    }
}