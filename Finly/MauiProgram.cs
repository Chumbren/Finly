using CommunityToolkit.Maui.Storage;
using Finly.Converters;
using Finly.Services;
using Finly.ViewModels;
using Finly.Views;
using Finly.Views.ModelWindows;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Finly
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Регистрация сервисов
            builder.Services.AddSingleton<IDataService, LocalDataService>();
            builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();
            builder.Services.AddSingleton<IPdfExportService, PdfExportService>();
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
            // ViewModel'и как transient (новый экземпляр для каждой страницы)
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<TransactionsViewModel>();
            builder.Services.AddTransient<AddTransactionViewModel>();
            builder.Services.AddTransient<AccountsViewModel>();
            builder.Services.AddTransient<AddAccountViewModel>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<ExportOptionsViewModel>();

          
            // Страницы
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<AddTransactionPage>();
            builder.Services.AddTransient<AccountsPage>();
            builder.Services.AddTransient<AddAccountPage>();
            builder.Services.AddTransient<ExportOptionsPage>();
            builder.Services.AddTransient<AccountsPopup>();
            builder.Services.AddTransient<ReportsPage>();

            // Конвертеры
            builder.Services.AddSingleton<AmountColorConverter>();

            return builder.Build();
        }
    }
    }
