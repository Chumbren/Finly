using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finly.Services;
using Finly.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Finly.ViewModels;

public partial class ExportOptionsViewModel : ObservableObject
{
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private ReportsViewModel _reportsViewModel;

    [ObservableProperty]
    private string _selectedReportType;

    [ObservableProperty]
    private string _selectedFormat = "PDF";

    [ObservableProperty]
    private ObservableCollection<string> _reportTypes = new()
    {
        "Текущий отчет",
        "Только график",
        "Детализация по категориям",
        "Все операции",
        "Полный отчет за период"
    };

    public IRelayCommand CloseCommand { get; }
    public IRelayCommand ExportCommand { get; }

    public ExportOptionsViewModel(IPdfExportService pdfExportService, IExcelExportService excelExportService)
    {
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        SelectedReportType = "Текущий отчет";

        CloseCommand = new RelayCommand(Close);
        ExportCommand = new RelayCommand(Export);
    }

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
        try
        {
            Debug.WriteLine($"Экспорт: Формат={SelectedFormat}, Тип отчета={SelectedReportType}");

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
                Debug.WriteLine("Экспорт в PDF...");
                success = await _pdfExportService.ExportReportToPdfAsync(
                    _reportsViewModel.CurrentReport,
                    _reportsViewModel.ReportStartDate,
                    _reportsViewModel.ReportEndDate,
                    SelectedReportType
                );
            }
            else if (SelectedFormat == "Excel")
            {
                Debug.WriteLine("Экспорт в Excel...");
                success = await _excelExportService.ExportReportToExcelAsync(
                    _reportsViewModel.CurrentReport,
                    _reportsViewModel.ReportStartDate,
                    _reportsViewModel.ReportEndDate,
                    SelectedReportType
                );
            }

            if (!success)
            {
                Debug.WriteLine("Экспорт не удался");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    "Не удалось выполнить экспорт", "OK");
            }
            else
            {
                Debug.WriteLine("Экспорт успешно выполнен");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка в Export: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Ошибка",
                $"Ошибка экспорта: {ex.Message}", "OK");
        }
    }
}