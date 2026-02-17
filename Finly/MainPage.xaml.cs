using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Finly.Models;
using Finly.Services;
using Finly.ViewModels;
using Finly.Views;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace Finly
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadDashboardDataCommand.Execute(null);
        }

        private async void OnTransactionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
            if (e.CurrentSelection.FirstOrDefault() is TransactionDisplayItem displayItem)
            {
                await Shell.Current.GoToAsync($"///{nameof(AddTransactionPage)}?TransactionId={displayItem.Transaction.Id}");
            }
        }

        [RelayCommand]
        private async Task EditTransaction(TransactionDisplayItem displayItem)
        {
            if (displayItem?.Transaction != null)
            {
                await Shell.Current.GoToAsync($"{nameof(AddTransactionPage)}?TransactionId={displayItem.Transaction.Id}");
            }
        }

       
        
    }
}