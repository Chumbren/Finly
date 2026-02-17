using Finly.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Diagnostics;

namespace Finly.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty(nameof(TransactionId), nameof(TransactionId))]
    public partial class AddTransactionPage : ContentPage
    {
        private readonly AddTransactionViewModel _viewModel;
        private int? _transactionId;

        public string TransactionId
        {
            set
            {
                if (int.TryParse(value, out int id))
                {
                    _transactionId = id;
                }
            }
        }

        public AddTransactionPage(AddTransactionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (_transactionId.HasValue)
            {
                _viewModel.LoadDataCommand.Execute(_transactionId.Value);
            }
            else
            {
                _viewModel.LoadDataCommand.Execute(null);
            }
        }

        private void OnAmountTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                _viewModel.UpdateAmountValidation(entry.Text);
            }
        }

        private void OnDescriptionTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.UpdateDescriptionValidation();
        }

        private void OnCategorySelected(object sender, EventArgs e)
        {
            _viewModel.UpdateCategoryValidation();
        }

        private void OnAccountSelected(object sender, EventArgs e)
        {
            _viewModel.UpdateAccountValidation();
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            _viewModel.UpdateDateValidation();
        }
    }
}