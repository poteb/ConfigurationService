using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using Environment = pote.Config.Admin.Api.Model.Environment;

namespace pote.Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<EnvironmentsResponse> GetEnvironments();
        Task<SystemsResponse> GetSystems();
        Task SaveEnvironments(List<Environment> toApi);
    }

    public class AdminApiService : IAdminApiService
    {
        private readonly HttpClient _client;

        public AdminApiService(HttpClient client)
        {
            _client = client;
        }

        public async Task<EnvironmentsResponse> GetEnvironments()
        {
            var response = await _client.GetAsync("Environments");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EnvironmentsResponse>() ?? new EnvironmentsResponse();
            }

            return new EnvironmentsResponse();
        }

        public async Task<SystemsResponse> GetSystems()
        {
            var response = await _client.GetAsync("Systems");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SystemsResponse>() ?? new SystemsResponse();
            }

            return new SystemsResponse();
        }

        public async Task SaveEnvironments(List<Environment> environments)
        {
            foreach (var environment in environments)
            {
                await _client.PostAsJsonAsync("Environments", environment);
            }
        }
    }
}