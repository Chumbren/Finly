using Finly.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Finly.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }



    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var info = e.Info;
        _viewModel.DrawChart(canvas, info.Width, info.Height);
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.GenerateReportCommand.Execute(null);
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ReportsViewModel.CurrentReport) ||
                e.PropertyName == nameof(ReportsViewModel.ChartType))
            {
                ChartCanvas?.InvalidateSurface();
            }
        };
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        ChartCanvas?.InvalidateSurface();
    }
}