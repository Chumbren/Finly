using Finly.ViewModels;

namespace Finly.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Установка максимальной даты (обход ошибки XLS0414)
        var today = DateTime.Today;
        _viewModel.StartDate = today.AddDays(-30);
        _viewModel.EndDate = today;

        _viewModel.LoadDataCommand.Execute(null);
    }
}