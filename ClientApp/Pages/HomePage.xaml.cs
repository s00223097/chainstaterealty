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

/*
What I used:
- Figma to C# / XAML Figma plugin called Uno Platforms

Roughly followed this tutorial: https://platform.uno/docs/articles/external/workshops/simple-calc/modules/MVVM-XAML/02-Import%20UI%20from%20Figma/README.html

*/