﻿
using ClientApp.Pages;
using Shared;

namespace ClientApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Services = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var apiService = Services.GetRequiredService<ApiService>();
            return new Window(new NavigationPage(new MainPage(apiService)));
        }
    }
}
