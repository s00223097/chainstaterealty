using Shared;

namespace ClientApp.Pages;

public partial class LoginPage : ContentPage
{
	private ApiService apiService;

    public LoginPage(ApiService service)
	{
		InitializeComponent();

		apiService = service;
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		string? token = await apiService.Login(txtEmail.Text, txtPassword.Text);

		if (token != null)
		{
			var page = App.Services.GetRequiredService<HomePage>();
			await Navigation.PushAsync(page);
		}
	}
}