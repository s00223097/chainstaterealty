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
    }
}
