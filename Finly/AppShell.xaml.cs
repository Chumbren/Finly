using Finly.Views;
using Microsoft.Maui.Controls;

namespace Finly
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
           Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(TransactionsPage), typeof(TransactionsPage));
            
            InitializeComponent();
        }
    }
}