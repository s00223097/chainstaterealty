using Shared;

namespace ClientApp.Pages;

public partial class RegisterPage : ContentPage
{
	private ApiService apiService;

	public RegisterPage(ApiService service)
	{
		InitializeComponent();
		apiService = service;
	}

	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		btnRegister.IsEnabled = false;
		try
		{
			// Basic validation
			if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text) || string.IsNullOrEmpty(txtConfirmPassword.Text))
			{
				await DisplayAlert("Error", "Please fill in all fields", "OK");
				return;
			}

			if (txtPassword.Text != txtConfirmPassword.Text)
			{
				await DisplayAlert("Error", "Passwords do not match", "OK");
				return;
			}

			if (txtPassword.Text.Length < 6)
			{
				await DisplayAlert("Error", "Password must be at least 6 characters", "OK");
				return;
			}

			bool success = await apiService.Register(txtEmail.Text, txtPassword.Text);

			if (success)
			{
				await DisplayAlert("Success", "Account created successfully", "OK");
				
				// Navigate to login page
				await Navigation.PopAsync();
			}
			else
			{
				await DisplayAlert("Registration Failed", "Could not create account. Email may already be in use.", "Try Again");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
		finally
		{
			btnRegister.IsEnabled = true;
		}
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}