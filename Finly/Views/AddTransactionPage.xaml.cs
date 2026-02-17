using Finly.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Diagnostics;

namespace Finly.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty(nameof(TransactionId), nameof(TransactionId))] // 🔑 Обязательно для получения параметров
    public partial class AddTransactionPage : ContentPage
    {
        private readonly AddTransactionViewModel _viewModel;
        private int? _transactionId;

        // 🔑 Свойство для получения параметра из URL
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

            // 🔑 Загружаем данные в зависимости от наличия TransactionId
            if (_transactionId.HasValue)
            {
                _viewModel.LoadDataCommand.Execute(_transactionId.Value);
            }
            else
            {
                _viewModel.LoadDataCommand.Execute(null);
            }
        }
    }
}