using Finly.ViewModels;

namespace Finly.Services
{
    public interface IPdfExportService
    {
        Task<bool> ExportReportToPdfAsync(ReportData report, DateTime startDate, DateTime endDate, string reportType);
    }
}