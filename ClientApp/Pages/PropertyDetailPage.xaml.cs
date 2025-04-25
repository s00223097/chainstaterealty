using Shared;
using Shared.Model;

namespace ClientApp.Pages
{
    public partial class PropertyDetailPage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly Property _property;

        public PropertyDetailPage(ApiService apiService, Property property)
        {
            InitializeComponent();
            _apiService = apiService;
            _property = property;

            LoadPropertyDetails();
        }

        private void LoadPropertyDetails()
        {
            propertyName.Text = _property.Name;
            propertyAddress.Text = _property.Address;
            propertyDescription.Text = _property.Description;
            availableShares.Text = _property.AvailableShares.ToString();
            sharePrice.Text = $"${_property.SharePrice:N2}";
            imageCarousel.ItemsSource = _property.ImageUrls;
        }

        private async void OnInvestClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(sharesEntry.Text, out int shares) || shares <= 0)
            {
                await DisplayAlert("Error", "Please enter a valid number of shares", "OK");
                return;
            }

            if (shares > _property.AvailableShares)
            {
                await DisplayAlert("Error", "Not enough shares available", "OK");
                return;
            }

            try
            {
                var investment = new Investment
                {
                    PropertyId = _property.Id,
                    Shares = shares
                };

                var result = await _apiService.CreateInvestmentAsync(investment);
                if (result != null)
                {
                    await DisplayAlert("Success", "Investment created successfully", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to create investment", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create investment: {ex.Message}", "OK");
            }
        }
    }
} 