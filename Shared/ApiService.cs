using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Model;

namespace Shared
{
    public class ApiService
    {
        private HttpClient httpClient;

        public ApiService(Uri apiAddress)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = apiAddress;
        }

        public async Task<List<Test>> GetTestsAsync()
        {
            var response = await httpClient.GetFromJsonAsync<List<Test>>("/api/Tests");
            return response ?? new List<Test>();
        }

        public async Task<string?> Login(string email, string password)
        {
            try 
            {
                var model = new LoginModel { Email = email, Password = password };
                var response = await httpClient.PostAsJsonAsync("/api/auth/login", model);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return result?.Token;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> Register(string email, string password)
        {
            try
            {
                var model = new RegisterModel { 
                    Email = email, 
                    Password = password,
                    ConfirmPassword = password  // Adding this because the model requires it
                };
                
                var response = await httpClient.PostAsJsonAsync("/api/auth/register", model);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }
                else
                {
                    // Try to get detailed error message
                    string errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorDetails = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                        if (errorDetails?.Errors != null && errorDetails.Errors.Any())
                        {
                            return (false, string.Join(", ", errorDetails.Errors.Select(e => e.Description)));
                        }
                    }
                    catch
                    {
                        // Fallback if we can't parse the JSON error
                    }
                    
                    return (false, $"Registration failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public Uri GetSocialLoginUrl(SocialProvider provider, string returnUrl)
        {
            return new Uri($"{httpClient.BaseAddress}api/auth/login/{provider}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        public async Task<bool> VerifyToken(string token)
        {
            try
            {
                var model = new VerifyTokenModel { Token = token };
                var response = await httpClient.PostAsJsonAsync("/api/auth/verify-token", model);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> ExtractTokenFromUri(Uri uri)
        {
            // Example: myapp://login?token=xyz or https://myapp.com/callback?token=xyz
            if (uri != null && uri.Query.Contains("token="))
            {
                var query = uri.Query.TrimStart('?');
                var parameters = query.Split('&')
                    .Select(p => p.Split('='))
                    .ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : null);

                if (parameters.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token))
                {
                    return Uri.UnescapeDataString(token);
                }
            }
            
            return null;
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public IEnumerable<IdentityError> Errors { get; set; } = new List<IdentityError>();
    }

    public class IdentityError
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
