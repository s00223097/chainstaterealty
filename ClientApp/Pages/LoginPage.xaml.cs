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
		// Disable button during login attempt
		btnLogin.IsEnabled = false;
		try
		{
			if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text))
			{
				await DisplayAlert("Error", "Please enter both email and password", "OK");
				return;
			}

			string? token = await apiService.Login(txtEmail.Text, txtPassword.Text);

			if (token != null)
			{
				Preferences.Set("AuthToken", token);
				var page = App.Services.GetRequiredService<HomePage>();
				await Navigation.PushAsync(page);
			}
			else
			{
				await DisplayAlert("Login Failed", "Invalid email or password", "Try Again");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
		finally
		{
			btnLogin.IsEnabled = true;
		}
	}
	
	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		var page = App.Services.GetRequiredService<RegisterPage>();
		await Navigation.PushAsync(page);
	}
}