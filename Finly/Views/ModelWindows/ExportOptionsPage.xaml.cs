using Finly.ViewModels;

namespace Finly.Views.ModelWindows;

public partial class ExportOptionsPage : ContentPage
{
    public ExportOptionsPage(ExportOptionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}