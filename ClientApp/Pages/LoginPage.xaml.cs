using Shared;
using Shared.Model;
using Microsoft.Maui.ApplicationModel;

namespace ClientApp.Pages;

public partial class LoginPage : ContentPage
{
	private ApiService apiService;
	private bool isSocialLoginInProgress = false;

    public LoginPage(ApiService service)
	{
		InitializeComponent();

		apiService = service;
        
        // To handle social login callbacks
        Application.Current.Windows[0].Resumed += OnAppResumed;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unregister from events when the page is not shown anymore
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
                navPage.Navigation.NavigationStack.LastOrDefault() is LoginPage)
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
        
                    await DisplayAlert("Login Failed", "Could not authenticate with the social provider.", "Try Again");
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

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		// Disable button during login attempt and show loading overlay
		btnLogin.IsEnabled = false;
		loadingOverlay.IsVisible = true;
		
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
				await DisplayAlert("Login Failed", "Invalid email or password. Please check your credentials and try again.", "Try Again");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
		finally
		{
			loadingOverlay.IsVisible = false;
			btnLogin.IsEnabled = true;
		}
	}
	
	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		var page = App.Services.GetRequiredService<RegisterPage>();
		await Navigation.PushAsync(page);
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
            
            // callback URL - leaving to be localhost for now !!! CHANGE LATER
            string callbackUrl = "https://localhost/callback"; 
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