namespace ClientApp.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		var page = App.Services.GetRequiredService<HomePage>();
		await Navigation.PushAsync(page);
	}
}