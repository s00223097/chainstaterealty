using Shared;
using Shared.Model;

namespace ClientApp.Pages;

public partial class HomePage : ContentPage
{
	private ApiService service;

	public HomePage(ApiService apiService)
	{
		InitializeComponent();

		service = apiService;
	}

    protected override async void OnAppearing()
    {
		List<Test> tests = await service.GetTestsAsync();

		foreach (Test test in tests)
		{
			System.Diagnostics.Debug.WriteLine(test.Name);
		}

        base.OnAppearing();
    }
}