using Shared;
using Shared.Model;

namespace ClientApp.Pages
{
    public partial class InvestmentsPage : ContentPage
    {
        private readonly ApiService _apiService;
        private List<Investment> _investments = new();

        public InvestmentsPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadInvestments();
        }

        private async Task LoadInvestments()
        {
            try
            {
                _investments = await _apiService.GetInvestmentsAsync();
                investmentsCollection.ItemsSource = _investments;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load investments: {ex.Message}", "OK");
            }
        }

        private async void OnInvestmentSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Investment selectedInvestment)
            {
                var result = await DisplayAlert("Investment Details", 
                    $"Property: {selectedInvestment.Property.Name}\n" +
                    $"Shares: {selectedInvestment.Shares}\n" +
                    $"Total Investment: ${selectedInvestment.TotalInvestment:N2}\n" +
                    $"Purchase Date: {selectedInvestment.PurchaseDate:d}\n\n" +
                    "Would you like to sell your shares?",
                    "Yes", "No");

                if (result)
                {
                    await SellInvestment(selectedInvestment);
                }

                investmentsCollection.SelectedItem = null;
            }
        }

        private async Task SellInvestment(Investment investment)
        {
            try
            {
                var success = await _apiService.DeleteInvestmentAsync(investment.Id);
                if (success)
                {
                    await DisplayAlert("Success", "Investment sold successfully", "OK");
                    await LoadInvestments();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to sell investment", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to sell investment: {ex.Message}", "OK");
            }
        }

        public Command RefreshCommand => new Command(async () =>
        {
            await LoadInvestments();
            refreshView.IsRefreshing = false;
        });
    }
} 