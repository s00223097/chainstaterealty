using Shared;
using Shared.Model;
using Microsoft.Maui.ApplicationModel;

namespace ClientApp.Pages;

public partial class RegisterPage : ContentPage
{
	private ApiService apiService;
	private bool isPasswordValid = false;
	private bool doPasswordsMatch = false;
	private bool isSocialLoginInProgress = false;

	public RegisterPage(ApiService service)
	{
		InitializeComponent();
		apiService = service;
		
		// Register for app resuming events to handle social login callbacks
        Application.Current.Windows[0].Resumed += OnAppResumed;
	}
	
	protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unregister from events when the page is no longer visible
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Resumed -= OnAppResumed;
        }
    }
    
    private async void OnAppResumed(object sender, EventArgs e)
    {
        if (!isSocialLoginInProgress)
            return;
            
        isSocialLoginInProgress = false;
        
        try
        {
            // Check if we have a pending URL with token
            if (Application.Current?.MainPage is NavigationPage navPage &&
                navPage.Navigation.NavigationStack.LastOrDefault() is RegisterPage)
            {
                // Get the app launcher URL if available
                var url = await SecureStorage.GetAsync("social_login_callback");
                
                if (!string.IsNullOrEmpty(url))
                {
                    await SecureStorage.SetAsync("social_login_callback", string.Empty);
                    
                    var uri = new Uri(url);
                    var token = await apiService.ExtractTokenFromUri(uri);
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Verify token and login
                        bool isValid = await apiService.VerifyToken(token);
                        
                        if (isValid)
                        {
                            Preferences.Set("AuthToken", token);
                            var page = App.Services.GetRequiredService<HomePage>();
                            await Navigation.PushAsync(page);
                            return;
                        }
                    }
                    
                    // Show error if token extraction failed
                    await DisplayAlert("Registration Failed", "Could not register with the social provider.", "Try Again");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            loadingOverlay.IsVisible = false;
        }
    }

	private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
	{
		// Could add email validation feedback here
	}

	private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
	{
		string password = txtPassword.Text ?? string.Empty;
		
		bool isLengthValid = password.Length >= 6;
		
		if (isLengthValid)
		{
			lblPasswordRequirements.Text = "Password meets requirements";
			lblPasswordRequirements.TextColor = Colors.Green;
			isPasswordValid = true;
		}
		else
		{
			lblPasswordRequirements.Text = "Password must be at least 6 characters";
			lblPasswordRequirements.TextColor = Colors.Gray;
			isPasswordValid = false;
		}
		
		// Check match with confirm password if it's not empty
		CheckPasswordsMatch();
	}

	private void OnConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
	{
		CheckPasswordsMatch();
	}
	
	private void CheckPasswordsMatch()
	{
		string password = txtPassword.Text ?? string.Empty;
		string confirmPassword = txtConfirmPassword.Text ?? string.Empty;
		
		if (!string.IsNullOrEmpty(confirmPassword))
		{
			lblPasswordMatch.IsVisible = true;
			
			if (password == confirmPassword)
			{
				lblPasswordMatch.Text = "Passwords match";
				lblPasswordMatch.TextColor = Colors.Green;
				doPasswordsMatch = true;
			}
			else
			{
				lblPasswordMatch.Text = "Passwords do not match";
				lblPasswordMatch.TextColor = Colors.Red;
				doPasswordsMatch = false;
			}
		}
		else
		{
			lblPasswordMatch.IsVisible = false;
			doPasswordsMatch = false;
		}
	}

	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		btnRegister.IsEnabled = false;
		loadingOverlay.IsVisible = true;
		
		try
		{
			// Basic validation
			if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text) || string.IsNullOrEmpty(txtConfirmPassword.Text))
			{
				await DisplayAlert("Error", "Please fill in all fields", "OK");
				return;
			}

			if (!doPasswordsMatch)
			{
				await DisplayAlert("Error", "Passwords do not match", "OK");
				return;
			}

			if (!isPasswordValid)
			{
				await DisplayAlert("Error", "Password must be at least 6 characters", "OK");
				return;
			}

			var result = await apiService.Register(txtEmail.Text, txtPassword.Text);

			if (result.Success)
			{
				await DisplayAlert("Success", "Account created successfully", "OK");
				
				// Auto-login the user after successful registration
				var token = await apiService.Login(txtEmail.Text, txtPassword.Text);
				if (token != null)
				{
					Preferences.Set("AuthToken", token);
					// Navigate to home page
					var homePage = App.Services.GetRequiredService<HomePage>();
					await Navigation.PushAsync(homePage);
				}
				else
				{
					// Just go back to login page if auto-login fails
					await Navigation.PopAsync();
				}
			}
			else
			{
				
				string errorMessage = "Could not create account. ";
				if (!string.IsNullOrEmpty(result.ErrorMessage))
				{
					errorMessage += result.ErrorMessage;
				}
				else
				{
					errorMessage += "Email may already be in use.";
				}
				
				await DisplayAlert("Registration Failed", errorMessage, "Try Again");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
		finally
		{
			loadingOverlay.IsVisible = false;
			btnRegister.IsEnabled = true;
		}
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
	
	private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        await InitiateSocialLogin(SocialProvider.Google);
    }
    
    private async void OnMicrosoftLoginClicked(object sender, EventArgs e)
    {
        await InitiateSocialLogin(SocialProvider.Microsoft);
    }
    
    private async Task InitiateSocialLogin(SocialProvider provider)
    {
        try
        {
            loadingOverlay.IsVisible = true;
            isSocialLoginInProgress = true;
            
            // Determine callback URL - leaving to be localhost for now !!! CHANGE LATER
            string callbackUrl = "https://localhost/callback"; // This should be changed to your app's URL scheme
            var loginUrl = apiService.GetSocialLoginUrl(provider, callbackUrl);
            await SecureStorage.SetAsync("social_login_callback", callbackUrl);
            await Browser.OpenAsync(loginUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            isSocialLoginInProgress = false;
            loadingOverlay.IsVisible = false;
            await DisplayAlert("Error", $"Failed to initiate social login: {ex.Message}", "OK");
        }
    }
}