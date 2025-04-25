using ClientApp.Pages;
using Shared;

namespace ClientApp
{
    public partial class MainPage : ContentPage
    {
        private readonly ApiService _apiService;

        public MainPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(_apiService));
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage(_apiService));
        }
    }

}
