using Finly.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Finly
{
    public partial class App : Application
    {
        private readonly IDataService _dataService;

        public App(IDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;

        }

        protected override void OnStart()
        { 
            base.OnStart();
            _ = _dataService.InitializeAsync();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}