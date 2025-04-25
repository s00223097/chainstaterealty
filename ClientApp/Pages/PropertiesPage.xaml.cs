using Shared;
using Shared.Model;

namespace ClientApp.Pages
{
    public partial class PropertiesPage : ContentPage
    {
        private readonly ApiService _apiService;
        private List<Property> _properties = new();

        public PropertiesPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProperties();
        }

        private async Task LoadProperties()
        {
            try
            {
                _properties = await _apiService.GetPropertiesAsync();
                propertiesCollection.ItemsSource = _properties;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load properties: {ex.Message}", "OK");
            }
        }

        private async void OnPropertySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Property selectedProperty)
            {
                var propertyDetailPage = new PropertyDetailPage(_apiService, selectedProperty);
                await Navigation.PushAsync(propertyDetailPage);
                propertiesCollection.SelectedItem = null;
            }
        }

        public Command RefreshCommand => new Command(async () =>
        {
            await LoadProperties();
            refreshView.IsRefreshing = false;
        });
    }
} 