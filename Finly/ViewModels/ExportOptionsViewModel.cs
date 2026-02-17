using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Services;
using Finly.Views;

namespace Finly.ViewModels;

public partial class ExportOptionsViewModel : ObservableObject
{
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService; // Добавляем
    private ReportsViewModel _reportsViewModel;

    [ObservableProperty]
    private string _selectedReportType;

    [ObservableProperty]
    private string _selectedFormat = "PDF";

    public IRelayCommand CloseCommand { get; }
    public IRelayCommand ExportCommand { get; }

    // Обновляем конструктор
    public ExportOptionsViewModel(IPdfExportService pdfExportService, IExcelExportService excelExportService)
    {
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        SelectedReportType = "Текущий отчет";

        CloseCommand = new RelayCommand(Close);
        ExportCommand = new RelayCommand(Export);
    }

    // Метод инициализации (вызывается после создания через DI)
    public void Initialize(ReportsViewModel reportsViewModel)
    {
        _reportsViewModel = reportsViewModel;
    }

    private async void Close()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async void Export()
    {
            // Закрываем модальное окно
            await Shell.Current.Navigation.PopModalAsync();

            if (_reportsViewModel == null)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось получить данные отчета", "OK");
                return;
            }

            bool success = false;

            if (SelectedFormat == "PDF")
            {
                success = await _pdfExportService.ExportReportToPdfAsync(
                    _reportsViewModel.CurrentReport,
                    _reportsViewModel.ReportStartDate,
                    _reportsViewModel.ReportEndDate,
                    SelectedReportType
                );
            }
            else if (SelectedFormat == "Excel")
            {
                success = await _excelExportService.ExportReportToExcelAsync(
                    _reportsViewModel.CurrentReport,
                    _reportsViewModel.ReportStartDate,
                    _reportsViewModel.ReportEndDate,
                    SelectedReportType
                );
            }

            if (!success)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    "Не удалось выполнить экспорт", "OK");
            }
    }
}