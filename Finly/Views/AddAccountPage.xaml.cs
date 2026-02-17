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

            // Передаем ID счета в ViewModel
            if (_accountId.HasValue)
            {
                // Для редактирования: создаем ViewModel с ID
                // (Это уже сделано при регистрации маршрута)
                _viewModel.LoadAccountDataCommand.Execute(null);
            }
            else
            {
                // Для нового счета
                _viewModel.LoadAccountDataCommand.Execute(null);
            }
        }
    }
}