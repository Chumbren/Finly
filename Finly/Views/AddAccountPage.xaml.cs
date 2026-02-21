using Finly.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Finly.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty(nameof(AccountId), nameof(AccountId))]
    public partial class AddAccountPage : ContentPage
    {
        private readonly AddAccountViewModel _viewModel;
        private int? _accountId;

        public string AccountId
        {
            set
            {
                if (int.TryParse(value, out int id))
                {
                    _accountId = id;
                }
            }
        }

        public AddAccountPage(AddAccountViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (_accountId.HasValue)
            {
                _viewModel.LoadAccountDataCommand.Execute(_accountId.Value);
            }
            else
            {
                _viewModel.LoadAccountDataCommand.Execute(null);
            }
        }

        // Обработчик для изменения текста названия счета
        private void OnNameTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.UpdateNameValidation();
        }

        // Обработчик для выбора типа счета
        private void OnTypeSelected(object sender, EventArgs e)
        {
            _viewModel.UpdateTypeValidation();
        }

        // Обработчик для изменения баланса
        private void OnBalanceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                _viewModel.UpdateBalanceValidation(entry.Text);
            }
        }
    }
}