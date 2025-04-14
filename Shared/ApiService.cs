using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

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

        public async Task<bool> Register(string email, string password)
        {
            try
            {
                var model = new RegisterModel { Email = email, Password = password };
                var response = await httpClient.PostAsJsonAsync("/api/auth/register", model);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }
}
