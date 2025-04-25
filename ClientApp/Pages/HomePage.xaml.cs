using Shared;
using Shared.Model;

namespace ClientApp.Pages;

public partial class HomePage : ContentPage
{
	private readonly ApiService _apiService;

	public HomePage(ApiService apiService)
	{
		InitializeComponent();
		_apiService = apiService;
	}

	private async void OnBrowsePropertiesClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PropertiesPage(_apiService));
	}

	private async void OnMyInvestmentsClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new InvestmentsPage(_apiService));
	}

	protected override async void OnAppearing()
	{
		List<Test> tests = await _apiService.GetTestsAsync();

		foreach (Test test in tests)
		{
			System.Diagnostics.Debug.WriteLine(test.Name);
		}

		base.OnAppearing();
	}
}